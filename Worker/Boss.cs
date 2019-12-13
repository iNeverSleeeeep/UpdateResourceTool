
using Nirvana;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace URT
{
    internal class Boss
    {
        private static Boss instance;
        public static Boss Instance
        {
            get
            {
                if (instance == null)
                    instance = new Boss();
                return instance;
            }
        }

        private Dictionary<Project, List<Worker>> m_IdleWorkers = new Dictionary<Project, List<Worker>>();
        private Dictionary<Project, List<Worker>> m_BusyWorkers = new Dictionary<Project, List<Worker>>();
        private Dictionary<Project, Coroutine> m_Projects = new Dictionary<Project, Coroutine>();

        public void AddWorker(Project project, Worker worker)
        {
            List<Worker> workers = null;
            if (m_IdleWorkers.TryGetValue(project, out workers) == false)
            {
                workers = new List<Worker>();
                m_IdleWorkers.Add(project, workers);
            }
            workers.Add(worker);
        }

        public void StartProject(Project project, URTEvent.ProjectCompleteDelegate onCompolete = null)
        {
            if (m_Projects.ContainsKey(project))
                return;
            List<Worker> workers = null;
            if (m_IdleWorkers.TryGetValue(project, out workers))
            {
                var coroutine = Scheduler.RunCoroutine(ProjectWorking(project, workers, onCompolete));
                m_Projects.Add(project, coroutine);
            }
        }

        public void StopProject(Project project)
        {
            Coroutine coroutine;
            if (m_Projects.TryGetValue(project, out coroutine))
            {
                Scheduler.KillCoroutine(coroutine);
                m_Projects.Remove(project);
            }

            List<Worker> workers;
            if (m_BusyWorkers.TryGetValue(project, out workers))
            {
                foreach (var worker in workers)
                    worker.Dispose();
                m_BusyWorkers.Remove(project);
            }
            
            if (m_IdleWorkers.TryGetValue(project, out workers))
                m_IdleWorkers.Remove(project);
        }

        public void StopAllProject()
        {
            foreach (var project in m_Projects)
                Scheduler.KillCoroutine(project.Value);
            m_Projects.Clear();
            foreach (var project in m_BusyWorkers)
                foreach (var worker in project.Value)
                    worker.Dispose();

            m_BusyWorkers.Clear();
            m_IdleWorkers.Clear();
        }

        private IEnumerator ProjectWorking(Project project, List<Worker> idleWorkers, URTEvent.ProjectCompleteDelegate onCompolete)
        {
            var busyWorkers = new List<Worker>();
            m_BusyWorkers.Add(project, busyWorkers);
            while (true)
            {
                for (var i = busyWorkers.Count - 1; i >= 0; --i)
                {
                    var worker = busyWorkers[i];
                    worker.Update();
                    if (worker.keepWaiting == false)
                        busyWorkers.RemoveAt(i);
                }

                while (busyWorkers.Count < URTConfig.MAX_BUSY_WORKERS && idleWorkers.Count > 0)
                {
                    var worker = idleWorkers[idleWorkers.Count - 1];
                    idleWorkers.RemoveAt(idleWorkers.Count - 1);
                    busyWorkers.Add(worker);
                    worker.Run();
                }

                yield return null;
                if (busyWorkers.Count  == 0)
                {
                    m_Projects.Remove(project);
                    if (onCompolete != null)
                        onCompolete(ErrorCode.SUCCESS, "");
                    break;
                }
            }
        }
    }
}
