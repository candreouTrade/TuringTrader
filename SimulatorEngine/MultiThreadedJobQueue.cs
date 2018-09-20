﻿//==============================================================================
// Project:     Trading Simulator
// Name:        MultiThreadedJobQueue
// Description: multi-threaded job queue to support optimizer
// History:     2018ix20, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

//#define NO_THREADS
// when NO_THREADS is defined, QueueJob translates to a plain function call

#region libraries
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    class MultiThreadedJobQueue
    {
        #region internal data
        private readonly object _queueLock = new object();
        private readonly Queue<Thread> _jobQueue = new Queue<Thread>();
        private int _jobsRunning = 0;
        #endregion

        #region private int NumberOfLogicalProcessors
        private int NumberOfLogicalProcessors
        {
            get
            {
                // https://stackoverflow.com/questions/1542213/how-to-find-the-number-of-cpu-cores-via-net-c
                return Environment.ProcessorCount;
            }
        }
        #endregion
        #region private void CheckQueue()
        private void CheckQueue()
        {
            Thread nextThread = null;

            lock(_queueLock)
            {
                if (_jobsRunning < NumberOfLogicalProcessors
                && _jobQueue.Count > 0)
                {
                    nextThread = _jobQueue.Dequeue();
                    _jobsRunning++;
                }
            }

            if (nextThread != null)
                nextThread.Start();
        }
        #endregion
        #region private void JobRunner(Action job)
        private void JobRunner(Action job)
        {
            job();

            lock(_queueLock)
            {
                _jobsRunning--;
            }

            CheckQueue();
        }
        #endregion

        #region public void QueueJob(Action job)
        public void QueueJob(Action job)
        {
#if NO_THREADS
            job();
#else
            lock(_queueLock)
            {
                Thread queuedThread = new Thread(() => JobRunner(job));
                _jobQueue.Enqueue(queuedThread);
            }

            CheckQueue();
#endif
        }
        #endregion
        #region public void WaitForCompletion()
        public void WaitForCompletion()
        {
#if NO_THREADS
            // nothing to do
#else
            int? jobsToDo = null;

            do
            {
                if (jobsToDo != null)
                    Thread.Sleep(500);

                lock (_queueLock)
                {
                    jobsToDo = _jobQueue.Count + _jobsRunning;
                }
            } while (jobsToDo > 0);
#endif
        }
        #endregion
    }
}

//==============================================================================
// end of file