using System.Text;
using Chanlange.ListNodeSerializer.Interfaces;
using Chanlange.ListNodeSerializer.Nodes;

namespace Chanlange.ListNodeSerializer
{
	public class MySerializer : IListSerializer
	{
		// A parameterless constructor is required
		public MySerializer()
		{ }

		/// <summary>
		/// Serializes the doubly-linked list (including random links) into a stream.
		/// Format for each node: [DataLength(int), Data(string chars), RandomIndex(int)]
		/// RandomIndex is -1 if the Random reference is null.
		/// Time - O(n)
		/// Space - O(n), O(n+M) - where M is the total length of the Data fields in all nodes.
		/// </summary>
		/// <param name="head">Head of the doubly-linked list</param>
		/// <param name="stream">Target stream</param>
		public async Task Serialize(ListNode head, Stream stream)
		{
			// Collect all nodes in a list by traversing from head to tail
			var nodes = new List<ListNode>();
			var nodeIndex = new Dictionary<ListNode, int>();

			var current = head;
			int index = 0;
			while (current != null)
			{
				nodes.Add(current);
				nodeIndex[current] = index;
				index++;
				current = current.Next;
			}

			// Write the data to the stream:
			// 1) total number of nodes
			// 2) for each node: data length, data, random index
			using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
			{
				writer.Write(nodes.Count); // total number of nodes

				foreach (var node in nodes)
				{
					// Handle Data: if null => write -1
					if (node.Data == null)
					{
						writer.Write(-1);
					}
					else
					{
						// Write the Data field
						string data = node.Data ?? string.Empty;
						writer.Write(data.Length);
						writer.Write(data.ToCharArray());
					}

					// Write the Random index (-1 if no Random)
					if (node.Random == null)
					{
						writer.Write(-1);
					}
					else
					{
						writer.Write(nodeIndex[node.Random]);
					}
				}
			}

			// Complete the Task (no actual async work here)
			await Task.CompletedTask;
		}

		/// <summary>
		/// Deserializes the list from the given stream, restoring the Next, Previous and Random links.
		/// Time - O(count+M). That is, a linear dependence on the number of nodes(and the sum of the row sizes).
		/// Space - O(n) 
		/// </summary>
		/// <param name="stream">Source stream</param>
		/// <returns>Head of the reconstructed list</returns>
		public async Task<ListNode> Deserialize(Stream stream)
		{
			using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
			// Read the total number of nodes
			int count = reader.ReadInt32();
			if (count == 0)
			{
				// Empty list
				return null;
			}

			// Prepare arrays for new nodes and their random indices
			var newNodes = new ListNode[count];
			var randomIndexes = new int[count];

			// Create all nodes first (without links), read data for each
			for (int i = 0; i < count; i++)
			{
				newNodes[i] = new ListNode();

				// Read the data length and data string
				int dataLength = reader.ReadInt32();
				// Handle Data: if -1 => write null
				if (dataLength == -1)
				{
					newNodes[i].Data = null;
				}
				else
				{
					char[] dataChars = reader.ReadChars(dataLength);
					string data = new(dataChars);
					newNodes[i].Data = data;
				}
				// Read the random index for later restoration
				int randomIndex = reader.ReadInt32();
				randomIndexes[i] = randomIndex;
			}

			// Link the nodes in a doubly-linked fashion
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

			// Restore the Random links
			for (int i = 0; i < count; i++)
			{
				int rIndex = randomIndexes[i];
				if (rIndex >= 0 && rIndex < count)
				{
					newNodes[i].Random = newNodes[rIndex];
				}
			}

			// The head of the list is the first node
			return await Task.FromResult(newNodes[0]);
		}

		/// <summary>
		/// Creates a deep copy of the entire list using the classic two-phase approach:
		///  1) For each original node, create a copy node right after it. 
		///  2) Set up the Random links for each copied node. O(n)
		///  3) Separate the copied nodes into a new list, fixing Next/Previous links.
		///  Time - O(n)
		///  Space - O(n)
		/// </summary>
		/// <param name="head">Head of the original list</param>
		/// <returns>Head of the newly copied list</returns>
		public async Task<ListNode> DeepCopy(ListNode head)
		{
			if (head == null)
			{
				return null;
			}

			// Phase 1: Create a copy node and place it immediately after each original node
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
				// We'll fix the copy.Previous later during separation
				current = copy.Next;
			}
			//We have here next subsequence: original1 -> copy1 -> original2 -> copy2 -> ...

			// Phase 2: Fix Random links for the copied nodes
			current = head;
			while (current != null)
			{
				var copy = current.Next;
				if (current.Random != null)
				{
					// copy.Random should point to the copy of current.Random
					copy.Random = current.Random.Next;
				}
				current = copy.Next;
			}

			// Phase 3: Separate the two lists (original and the copied one)
			current = head;
			var newHead = head.Next; // The head of the new list is the copy of the first node
			while (current != null)
			{
				var copy = current.Next;
				var nextOriginal = copy.Next; // This is the next original node

				// Restore the next link for the original node
				current.Next = nextOriginal;

				// Restore Next/Previous for the copy node
				if (nextOriginal != null)
				{
					copy.Next = nextOriginal.Next; // The next copy is nextOriginal.Next
					copy.Previous = nextOriginal;   // Make sure to set the doubly-linked reference
				}
				else
				{
					copy.Next = null;
				}

				current = nextOriginal;
			}

			return await Task.FromResult(newHead);
		}
	}
}
