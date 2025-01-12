using System.Text;
using Chanlange.ListNodeSerializer.Interfaces;
using Chanlange.ListNodeSerializer.Nodes;

namespace Chanlange.ListNodeSerializer
{
	/// <summary>
	/// Async serializer that uses two-pass indexing for Random and encodes strings in UTF-8 with a clear byte length marker.
	/// </summary>
	public class MyMemoryOptimizedSerializer : IListSerializer
	{
		// A parameterless constructor is required
		public MyMemoryOptimizedSerializer()
		{
		}

		/// <summary>
		/// Asynchronously serializes the doubly-linked list (including Random links) into a stream.
		/// Format:
		///   1) Int32: total node count
		///   2) For each node (in head->tail order):
		///      - Int32: number of bytes in the UTF-8-encoded string, or -1 if Data == null
		///      - [that many bytes of Data in UTF-8]
		///      - Int32: RandomIndex (or -1 if null)
		/// 
		/// Time: O(n)  (two passes: indexing + writing)
		/// Memory: O(n) (dictionary of node->index)
		/// </summary>
		public async Task Serialize(ListNode head, Stream stream)
		{
			// If the list is empty, just write count=0 and return
			if (head == null)
			{
				await WriteInt32Async(stream, 0).ConfigureAwait(false);
				return;
			}

			// 1) Build Dictionary
			var nodeIndex = new Dictionary<ListNode, int>();
			int index = 0;
			var current = head;
			while (current != null)
			{
				nodeIndex[current] = index;
				index++;
				current = current.Next;
			}
			int count = index;

			// 2) Write the total count of nodes
			await WriteInt32Async(stream, count).ConfigureAwait(false);

			// 3) Traverse again and write data
			current = head;
			while (current != null)
			{
				if (current.Data == null)
				{
					// mark as -1 => null
					await WriteInt32Async(stream, -1).ConfigureAwait(false);
				}
				else
				{
					var utf8Bytes = Encoding.UTF8.GetBytes(current.Data);
					int byteCount = utf8Bytes.Length;

					// Write the length in bytes
					await WriteInt32Async(stream, byteCount).ConfigureAwait(false);
					// Write the actual data
					await stream.WriteAsync(utf8Bytes.AsMemory(0, byteCount)).ConfigureAwait(false);
				}

				// Write Random index
				int randomIndex = -1;
				if (current.Random != null)
				{
					randomIndex = nodeIndex[current.Random];
				}
				await WriteInt32Async(stream, randomIndex).ConfigureAwait(false);

				current = current.Next;
			}
		}

		/// <summary>
		/// Asynchronously deserializes from the stream, restoring Next/Previous/Random.
		/// Steps:
		///   - Read node count (int).
		///   - For each node: read DataLengthInBytes (int).
		///       -> if -1 => Data=null
		///       -> else read that many bytes, decode UTF-8 => Data
		///     then read RandomIndex (int).
		///   - Build an array of nodes in order, link them (double-linked),
		///     fix Random referencing.
		/// 
		/// Time: O(n)
		/// Memory: O(n) for the array, plus the strings created
		/// </summary>
		public async Task<ListNode> Deserialize(Stream stream)
		{
			// 1) Read total count
			int count = await ReadInt32Async(stream).ConfigureAwait(false);
			if (count == 0)
			{
				return null;
			}

			var newNodes = new ListNode[count];
			var randomIndexes = new int[count];

			// 2) For each node, read data + randomIndex
			for (int i = 0; i < count; i++)
			{
				newNodes[i] = new ListNode();

				// Read the length in bytes
				int byteCount = await ReadInt32Async(stream).ConfigureAwait(false);
				if (byteCount == -1)
				{
					newNodes[i].Data = null;
				}
				else
				{
					// Read the bytes, decode UTF-8 -> Data
					byte[] buffer = new byte[byteCount];
					await ReadExactlyAsync(stream, buffer, 0, byteCount).ConfigureAwait(false);
					string decoded = Encoding.UTF8.GetString(buffer);
					newNodes[i].Data = decoded;
				}

				// Random index
				int rIndex = await ReadInt32Async(stream).ConfigureAwait(false);
				randomIndexes[i] = rIndex;
			}

			// 3) Link them as doubly-linked
			for (int i = 0; i < count; i++)
			{
				if (i == 0)
				{
					newNodes[i].Previous = null;
					if (count > 1)
						newNodes[i].Next = newNodes[i + 1];
				}
				else
				{
					newNodes[i].Previous = newNodes[i - 1];
					newNodes[i - 1].Next = newNodes[i];
				}
			}

			// 4) Fix Random
			for (int i = 0; i < count; i++)
			{
				int rIdx = randomIndexes[i];
				if (rIdx >= 0 && rIdx < count)
				{
					newNodes[i].Random = newNodes[rIdx];
				}
			}

			// Return the head as first node
			return newNodes[0];
		}

		/// <summary>
		/// DeepCopy in-memory. We wrap the synchronous logic in Task.Run
		/// to keep the async signature. O(n) time & memory.
		/// </summary>
		public async Task<ListNode> DeepCopy(ListNode head)
		{
			if (head == null) return null;

			return await Task.Run(() =>
			{
				// 1) copy node after each original
				var current = head;
				while (current != null)
				{
					var copy = new ListNode
					{
						Data = current.Data,
						Next = current.Next,
						Previous = null,
						Random = null
					};
					current.Next = copy;
					current = copy.Next;
				}

				// 2) fix Random
				current = head;
				while (current != null)
				{
					var copy = current.Next;
					if (current.Random != null)
					{
						copy.Random = current.Random.Next;
					}
					current = copy.Next;
				}

				// 3) separate the two lists
				current = head;
				var newHead = head.Next;

				while (current != null)
				{
					var copy = current.Next;
					var nextOriginal = copy.Next;

					current.Next = nextOriginal;
					if (nextOriginal != null)
					{
						copy.Next = nextOriginal.Next;
						copy.Previous = nextOriginal;
					}
					else
					{
						copy.Next = null;
					}

					current = nextOriginal;
				}

				return newHead;
			}).ConfigureAwait(false);
		}

		#region Low-level Helpers

		/// <summary>
		/// Writes an Int32 (4 bytes) in little-endian format, asynchronously.
		/// </summary>
		private static async Task WriteInt32Async(Stream stream, int value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
		}

		/// <summary>
		/// Reads an Int32 (4 bytes) in little-endian format, asynchronously.
		/// Throws if end of stream.
		/// </summary>
		private static async Task<int> ReadInt32Async(Stream stream)
		{
			byte[] buffer = new byte[4];
			await ReadExactlyAsync(stream, buffer, 0, 4).ConfigureAwait(false);
			return BitConverter.ToInt32(buffer, 0);
		}

		/// <summary>
		/// Reads exactly count bytes into buffer[offset..offset+count]
		/// </summary>
		private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count)
		{
			int totalRead = 0;
			while (totalRead < count)
			{
				int readNow = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead).ConfigureAwait(false);
				if (readNow == 0)
					throw new EndOfStreamException("Stream ended before reading enough bytes.");
				totalRead += readNow;
			}
		}

		#endregion
	}
}
