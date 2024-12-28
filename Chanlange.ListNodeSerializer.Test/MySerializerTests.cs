using Chanlange.ListNodeSerializer.Nodes;

namespace Chanlange.ListNodeSerializer.Test
{
	public class MySerializerTests
	{
		[Fact]
		public void Test1()
		{
			// Arrange
			var serializer = new MySerializer();
			var head = BuildTestList();

			// ¿ÍÚ: Serialize -> Deserialize
			using var ms = new MemoryStream();
			serializer.Serialize(head, ms).GetAwaiter().GetResult();
			ms.Position = 0;
			var deserializedHead = serializer.Deserialize(ms).GetAwaiter().GetResult();

			// Assert #1: Check that the deserialized list is structurally the same
			Assert.True(CompareHelpers.AreListsEquivalent(head, deserializedHead),
				"Deserialized list should be equivalent to the original.");

			// ¿ÍÚ: DeepCopy
			var copiedHead = serializer.DeepCopy(head).GetAwaiter().GetResult();

			// Assert #2: Check that the deep-copied list is also structurally the same
			Assert.True(CompareHelpers.AreListsEquivalent(head, copiedHead),
				"Deep-copied list should be equivalent to the original.");

			// (Optional) Assert #3: Check that modifications to the copy don't affect the original
			copiedHead.Data = "Mutated Head Data";
			Assert.NotEqual(head.Data, copiedHead.Data);
		}

		/// <summary>
		/// Builds a small test list with random pointers for demonstration.
		/// </summary>
		private ListNode BuildTestList()
		{
			var node1 = new ListNode { Data = "Node 1" };
			var node2 = new ListNode { Data = "Node 2" };
			var node3 = new ListNode { Data = "Node 3" };

			// Link them in a doubly-linked chain: 1 <-> 2 <-> 3
			node1.Next = node2;
			node2.Previous = node1;
			node2.Next = node3;
			node3.Previous = node2;

			// Set random references
			// e.g. node1.Random -> node3, node2.Random -> node1, node3.Random -> null
			node1.Random = node3;
			node2.Random = node1;
			node3.Random = null;

			return node1; // head
		}
	}
}
