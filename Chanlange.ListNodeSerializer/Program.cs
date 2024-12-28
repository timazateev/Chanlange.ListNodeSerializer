using Chanlange.ListNodeSerializer.Nodes;

namespace Chanlange.ListNodeSerializer
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("Hello, World!");

			var serializer = new MySerializer();

			// Build a small list manually
			var node1 = new ListNode { Data = "Node 1" };
			var node2 = new ListNode { Data = "Node 2" };
			var node3 = new ListNode { Data = "Node 3" };

			node1.Next = node2; node2.Previous = node1;
			node2.Next = node3; node3.Previous = node2;

			// Set some random references
			node1.Random = node3;
			node2.Random = node1;
			node3.Random = null;

			var head = node1;

			// --- Serialize ---
			using var ms = new MemoryStream();
			await serializer.Serialize(head, ms);

			// Reset position for reading
			ms.Position = 0;

			// --- Deserialize ---
			var newHead = await serializer.Deserialize(ms);
			Console.WriteLine("Deserialization completed. Head data: " + newHead.Data);

			// --- DeepCopy ---
			var copiedHead = await serializer.DeepCopy(head);
			Console.WriteLine("Deep copy completed. Copied head data: " + copiedHead.Data);

			// Just to show it runs
			Console.WriteLine("Done. Press any key...");
			Console.ReadKey();
		}
	}
}
