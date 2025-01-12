using Chanlange.ListNodeSerializer.Nodes;
using System.Text;

namespace Chanlange.ListNodeSerializer
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			#region Simple start
			//var serializer = new MySerializer();

			//// Build a small list manually
			//var node1 = new ListNode { Data = "Node 1" };
			//var node2 = new ListNode { Data = "Node 2" };
			//var node3 = new ListNode { Data = "Node 3" };

			//node1.Next = node2; node2.Previous = node1;
			//node2.Next = node3; node3.Previous = node2;

			//// Set some random references
			//node1.Random = node3;
			//node2.Random = node1;
			//node3.Random = null;

			//var head = node1;

			//// --- Serialize ---
			//using var ms = new MemoryStream();
			//await serializer.Serialize(head, ms);

			//// Reset position for reading
			//ms.Position = 0;

			//// --- Deserialize ---
			//var newHead = await serializer.Deserialize(ms);
			//Console.WriteLine("Deserialization completed. Head data: " + newHead.Data);

			//// --- DeepCopy ---
			//var copiedHead = await serializer.DeepCopy(head);
			//Console.WriteLine("Deep copy completed. Copied head data: " + copiedHead.Data);

			//// Just to show it runs
			//Console.WriteLine("Done. Press any key...");
			//Console.ReadKey();
			// 1) Prompt the user for the file path

			#endregion

			#region Read data from file start

			Console.WriteLine("Please enter the path to the big test data file:");
			string filePath = Console.ReadLine();

			// 2) Validate the file path
			if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
			{
				Console.WriteLine("Invalid file path or file does not exist. Exiting...");
				return;
			}

			Console.WriteLine($"Reading file from: {filePath}");
			// 3) Build the doubly-linked list from the file
			var head = BuildListFromFile(filePath);

			// Optionally check how many lines / nodes we read
			int countOriginal = CountNodes(head);
			Console.WriteLine($"Finished reading. Total nodes: {countOriginal}");

			// 4) Prepare serializer and MemoryStream
			var serializer = new MySerializer();
			using var ms = new MemoryStream();

			// 5) Serialize
			var startSerialize = DateTime.Now;
			await serializer.Serialize(head, ms);
			var endSerialize = DateTime.Now;
			Console.WriteLine($"Serialization completed in {(endSerialize - startSerialize).TotalSeconds} seconds.");
			Console.WriteLine($"Serialized data size: {ms.Length} bytes.");

			// 6) Deserialize
			ms.Position = 0;
			var startDeserialize = DateTime.Now;
			var newHead = await serializer.Deserialize(ms);
			var endDeserialize = DateTime.Now;
			Console.WriteLine($"Deserialization completed in {(endDeserialize - startDeserialize).TotalSeconds} seconds.");

			int countDeserialized = CountNodes(newHead);
			Console.WriteLine($"Deserialized list node count: {countDeserialized}");

			// 7) DeepCopy
			var startCopy = DateTime.Now;
			var copyHead = await serializer.DeepCopy(head);
			var endCopy = DateTime.Now;
			Console.WriteLine($"DeepCopy completed in {(endCopy - startCopy).TotalSeconds} seconds.");

			int countCopy = CountNodes(copyHead);
			Console.WriteLine($"Copied list node count: {countCopy}");

			// (Optional) further checks, comparisons, etc.

			Console.WriteLine("All done. Press any key to exit.");
			Console.ReadKey();

			#endregion
		}

		/// <summary>
		/// Reads each line from the file, creates a ListNode for it,
		/// links them into a doubly-linked list, and sets Random pointers randomly.
		/// </summary>
		private static ListNode BuildListFromFile(string filePath)
		{
			ListNode head = null;
			ListNode prev = null;

			// We'll also store all nodes in a list to choose random references afterwards
			var allNodes = new List<ListNode>();

			using (var sr = new StreamReader(filePath, Encoding.UTF8))
			{
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					var node = new ListNode { Data = line };

					if (head == null)
					{
						head = node;
					}
					else
					{
						prev.Next = node;
						node.Previous = prev;
					}

					prev = node;
					allNodes.Add(node);
				}
			}

			// Now let's set Random pointers in a random manner
			Random rnd = new();
			int n = allNodes.Count;

			// Example approach: for each node, we pick a random index in [0, n), 
			// and set .Random to that node. 
			// You can add conditions if you want some nodes to have Random = null, etc.
			foreach (var node in allNodes)
			{
				// If you want to occasionally have Random = null, do something like:
				// if (rnd.NextDouble() < 0.1) { node.Random = null; continue; }

				int randomIndex = rnd.Next(n);   // random from 0 to n-1
				node.Random = allNodes[randomIndex];
			}

			return head;
		}

		/// <summary>
		/// Counts how many nodes are in a doubly-linked list via the Next pointer.
		/// </summary>
		private static int CountNodes(ListNode head)
		{
			int count = 0;
			var current = head;
			while (current != null)
			{
				count++;
				current = current.Next;
			}
			return count;
		}
	}
}
