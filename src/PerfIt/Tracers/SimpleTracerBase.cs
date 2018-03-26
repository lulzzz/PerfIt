﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PerfIt.Tracers
{
    /// <summary>
    /// A tracer that has a background thread committing PerfIt traces
    /// Since it has a dedicated thread, it does not need to be async - it does not steal from the ThreadPool
    /// </summary>
    public abstract class SimpleTracerBase : ITwoStageTracer
    {
        private Thread _worker;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private BlockingCollection<TraceData> _traceData = new BlockingCollection<TraceData>();

        public SimpleTracerBase()
        {
            _worker = new Thread(Work);
            _worker.IsBackground = true;
            _worker.Start();
        }

        public virtual void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }

        public int QueueDepth { get => _traceData.Count; }
        

        private void Work()
        {
            TraceData data = null;
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                bool isit = false;
                try
                {
                    isit = _traceData.TryTake(out data, 100, _cancellationTokenSource.Token);
                }
                catch
                {
                    // ignore cancellation
                }

                if(isit)
                    CommitTrace(data);
                    
            }
        }

        protected abstract void CommitTrace(TraceData data);

        public virtual void Finish(object token, long timeTakenMilli, string correlationId = null, InstrumentationContext extraContext = null)
        {
            _traceData.Add(new TraceData((IInstrumentationInfo)token, timeTakenMilli, correlationId, extraContext));
        }

        public virtual object Start(IInstrumentationInfo info)
        {
            return info;
        }
    }
}
