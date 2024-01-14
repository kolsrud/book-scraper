using System.Collections.Concurrent;

namespace BookScraper;

public class WorkerPool
{
	private readonly int _workerCount;
	private int _freeWorkers;

	private readonly Dictionary<int, Task> _workers = new Dictionary<int, Task>();
	private readonly Queue<Func<Task>> _queue = new Queue<Func<Task>>();
	private readonly BlockingCollection<Task> _completedTasks = new BlockingCollection<Task>();

	public WorkerPool(int workerCount)
	{
		_workerCount = workerCount;
		_freeWorkers = workerCount;
	}

	private static int taskId = 0;

	public void AddWork(Func<Task> job)
	{
		lock (_workers)
		{
			if (_freeWorkers == 0)
			{
				_queue.Enqueue(job);
				return;
			}

			lock (_workers)
			{
				var id = taskId++;
				_workers.Add(id, job().ContinueWith(t => OnTaskCompleted(id, t)));
				_freeWorkers--;
			}
		}
	}

	private void OnTaskCompleted(int id, Task t)
	{
		lock (_workers)
		{
			_workers.Remove(id);

			_freeWorkers++;
			if (_queue.Any())
			{
				var job = _queue.Dequeue();
				AddWork(job);
			}
		}

		_completedTasks.Add(t);
	}

	public bool IsFull => _freeWorkers == 0;
	public bool HasWork => _freeWorkers != _workerCount || _completedTasks.Any();
	public int CurrentLoad => _queue.Count + (_workerCount - _freeWorkers);

	public Task GetResult()
	{
		if (!HasWork) throw new Exception("No tasks running.");

		return _completedTasks.Take();
	}
}
