using System;
using System.Collections.Generic;
using System.Threading;
using Ionic.Zlib;

public class PlayFabZLibWorkQueue : IDisposable
{

	public PlayFabZLibWorkQueue()
	{
		PlayFabZLibWorkQueue.s_workersMutex.WaitOne();
		if (PlayFabZLibWorkQueue.s_thread == null)
		{
			PlayFabZLibWorkQueue.s_thread = new Thread(new ThreadStart(this.WorkerMain));
			PlayFabZLibWorkQueue.s_thread.Name = "PlayfabZlibThread";
			PlayFabZLibWorkQueue.s_thread.Start();
		}
		PlayFabZLibWorkQueue.s_workers.Add(this);
		PlayFabZLibWorkQueue.s_workersMutex.ReleaseMutex();
	}

	public void Compress(byte[] buffer)
	{
		this.m_buffersMutex.WaitOne();
		this.m_inCompress.Enqueue(buffer);
		this.m_buffersMutex.ReleaseMutex();
		if (PlayFabZLibWorkQueue.s_workSemaphore.CurrentCount < 1)
		{
			PlayFabZLibWorkQueue.s_workSemaphore.Release();
		}
	}

	public void Decompress(byte[] buffer)
	{
		this.m_buffersMutex.WaitOne();
		this.m_inDecompress.Enqueue(buffer);
		this.m_buffersMutex.ReleaseMutex();
		if (PlayFabZLibWorkQueue.s_workSemaphore.CurrentCount < 1)
		{
			PlayFabZLibWorkQueue.s_workSemaphore.Release();
		}
	}

	public void Poll(out List<byte[]> compressedBuffers, out List<byte[]> decompressedBuffers)
	{
		compressedBuffers = null;
		decompressedBuffers = null;
		this.m_buffersMutex.WaitOne();
		if (this.m_outCompress.Count > 0)
		{
			compressedBuffers = new List<byte[]>();
			while (this.m_outCompress.Count > 0)
			{
				compressedBuffers.Add(this.m_outCompress.Dequeue());
			}
		}
		if (this.m_outDecompress.Count > 0)
		{
			decompressedBuffers = new List<byte[]>();
			while (this.m_outDecompress.Count > 0)
			{
				decompressedBuffers.Add(this.m_outDecompress.Dequeue());
			}
		}
		this.m_buffersMutex.ReleaseMutex();
	}

	private void WorkerMain()
	{
		for (;;)
		{
			PlayFabZLibWorkQueue.s_workSemaphore.Wait();
			PlayFabZLibWorkQueue.s_workersMutex.WaitOne();
			foreach (PlayFabZLibWorkQueue playFabZLibWorkQueue in PlayFabZLibWorkQueue.s_workers)
			{
				playFabZLibWorkQueue.Execute();
			}
			PlayFabZLibWorkQueue.s_workersMutex.ReleaseMutex();
		}
	}

	private void Execute()
	{
		this.m_buffersMutex.WaitOne();
		this.DoUncompress();
		this.m_buffersMutex.ReleaseMutex();
		this.m_buffersMutex.WaitOne();
		this.DoCompress();
		this.m_buffersMutex.ReleaseMutex();
	}

	private void DoUncompress()
	{
		while (this.m_inDecompress.Count > 0)
		{
			try
			{
				byte[] array = this.m_inDecompress.Dequeue();
				byte[] array2 = this.UncompressOnThisThread(array);
				this.m_outDecompress.Enqueue(array2);
			}
			catch
			{
			}
		}
	}

	private void DoCompress()
	{
		while (this.m_inCompress.Count > 0)
		{
			try
			{
				byte[] array = this.m_inCompress.Dequeue();
				byte[] array2 = this.CompressOnThisThread(array);
				this.m_outCompress.Enqueue(array2);
			}
			catch
			{
			}
		}
	}

	public void Dispose()
	{
		PlayFabZLibWorkQueue.s_workersMutex.WaitOne();
		PlayFabZLibWorkQueue.s_workers.Remove(this);
		PlayFabZLibWorkQueue.s_workersMutex.ReleaseMutex();
	}

	internal byte[] CompressOnThisThread(byte[] payload)
	{
		return ZlibStream.CompressBuffer(payload, CompressionLevel.BestCompression);
	}

	internal byte[] UncompressOnThisThread(byte[] payload)
	{
		return ZlibStream.UncompressBuffer(payload);
	}

	private static Thread s_thread;

	private static bool s_moreWork;

	private static readonly List<PlayFabZLibWorkQueue> s_workers = new List<PlayFabZLibWorkQueue>();

	private readonly Queue<byte[]> m_inCompress = new Queue<byte[]>();

	private readonly Queue<byte[]> m_outCompress = new Queue<byte[]>();

	private readonly Queue<byte[]> m_inDecompress = new Queue<byte[]>();

	private readonly Queue<byte[]> m_outDecompress = new Queue<byte[]>();

	private static Mutex s_workersMutex = new Mutex();

	private Mutex m_buffersMutex = new Mutex();

	private static SemaphoreSlim s_workSemaphore = new SemaphoreSlim(0, 1);
}
