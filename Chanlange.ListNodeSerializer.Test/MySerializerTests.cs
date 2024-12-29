using System;
using System.IO;
using Xunit;
using Chanlange.ListNodeSerializer.Nodes;

namespace Chanlange.ListNodeSerializer.Test
{
	public class MySerializerTests
	{
		/// <summary>
		/// Basic test (original example):
		/// - Builds a small list.
		/// - Serializes -> Deserializes
		/// - DeepCopies
		/// - Checks structural equivalence and independence.
		/// </summary>
		[Fact]
		public void Test1()
		{
			// Arrange
			var serializer = new MySerializer();
			var head = BuildTestList();

			// Act #1: Serialize -> Deserialize
			using var ms = new MemoryStream();
			serializer.Serialize(head, ms).GetAwaiter().GetResult();
			ms.Position = 0;
			var deserializedHead = serializer.Deserialize(ms).GetAwaiter().GetResult();

			// Assert #1: Ensure the deserialized list is structurally equivalent
			Assert.True(CompareHelpers.AreListsEquivalent(head, deserializedHead),
				"Deserialized list should be equivalent to the original.");

			// Act #2: DeepCopy
			var copiedHead = serializer.DeepCopy(head).GetAwaiter().GetResult();

			// Assert #2: The deep-copied list is also structurally equivalent
			Assert.True(CompareHelpers.AreListsEquivalent(head, copiedHead),
				"Deep-copied list should be equivalent to the original.");

			// Optional Assert #3: Check that modifications in the copy do not affect the original
			copiedHead.Data = "Mutated Head Data";
			Assert.NotEqual(head.Data, copiedHead.Data);
		}

		/// <summary>
		/// 1) Empty list test.
		/// Ensures the serializer handles null (empty list) correctly.
		/// </summary>
		[Fact]
		public void EmptyListTest()
		{
			var serializer = new MySerializer();
			ListNode head = null;

			using var ms = new MemoryStream();
			serializer.Serialize(head, ms).GetAwaiter().GetResult();
			ms.Position = 0;
			var deserialized = serializer.Deserialize(ms).GetAwaiter().GetResult();

			Assert.Null(deserialized);

			var copied = serializer.DeepCopy(head).GetAwaiter().GetResult();
			Assert.Null(copied);
		}

		/// <summary>
		/// 2) Single-node list (Random = null).
		/// Checks basic Next/Previous/Random fields and deep copy independence.
		/// </summary>
		[Fact]
		public void SingleNodeTest()
		{
			var serializer = new MySerializer();
			var singleNode = new ListNode { Data = "Single", Random = null };

			using var ms = new MemoryStream();
			serializer.Serialize(singleNode, ms).GetAwaiter().GetResult();
			ms.Position = 0;
			var deserialized = serializer.Deserialize(ms).GetAwaiter().GetResult();

			Assert.NotNull(deserialized);
			Assert.Null(deserialized.Previous);
			Assert.Null(deserialized.Next);
			Assert.Null(deserialized.Random);
			Assert.Equal("Single", deserialized.Data);

			// DeepCopy
			var copied = serializer.DeepCopy(singleNode).GetAwaiter().GetResult();
			Assert.NotNull(copied);
			Assert.Equal("Single", copied.Data);
			Assert.Null(copied.Next);
			Assert.Null(copied.Previous);
			Assert.Null(copied.Random);

			// Ensure they are not the same object
			Assert.NotSame(singleNode, copied);
		}

		/// <summary>
		/// 2.1) Single-node list with Random pointing to itself.
		/// Ensures the serializer/deserializer restores self-referencing random.
		/// </summary>
		[Fact]
		public void SingleNodeSelfRandomTest()
		{
			var serializer = new MySerializer();
			var singleNode = new ListNode { Data = "SelfRandom" };
			singleNode.Random = singleNode; // Random -> self

			using var ms = new MemoryStream();
			serializer.Serialize(singleNode, ms).GetAwaiter().GetResult();
			ms.Position = 0;
			var deserialized = serializer.Deserialize(ms).GetAwaiter().GetResult();

			Assert.NotNull(deserialized);
			Assert.Equal("SelfRandom", deserialized.Data);

			// The Random should point to itself
			Assert.Same(deserialized, deserialized.Random);

			// DeepCopy
			var copied = serializer.DeepCopy(singleNode).GetAwaiter().GetResult();
			Assert.NotNull(copied);
			Assert.Equal("SelfRandom", copied.Data);
			Assert.Same(copied, copied.Random);
			Assert.NotSame(singleNode, copied);
		}

		/// <summary>
		/// 3) Various Random positions: 
		/// Random -> self, Random -> tail, Random -> null, etc.
		/// Ensures the serializer handles multiple scenarios in the same list.
		/// </summary>
		[Fact]
		public void RandomVariousPositionsTest()
		{
			var serializer = new MySerializer();

			var node1 = new ListNode { Data = "N1" };
			var node2 = new ListNode { Data = "N2" };
			var node3 = new ListNode { Data = "N3" };
			var node4 = new ListNode { Data = "N4" };

			// Link: 1 <-> 2 <-> 3 <-> 4
			node1.Next = node2; node2.Previous = node1;
			node2.Next = node3; node3.Previous = node2;
			node3.Next = node4; node4.Previous = node3;

			// Random setup
			node1.Random = node1; // self
			node2.Random = node4; // tail
			node3.Random = null;  // none
			node4.Random = node1; // back to head

			using var ms = new MemoryStream();
			serializer.Serialize(node1, ms).GetAwaiter().GetResult();
			ms.Position = 0;
			var deserializedHead = serializer.Deserialize(ms).GetAwaiter().GetResult();

			Assert.True(CompareHelpers.AreListsEquivalent(node1, deserializedHead),
				"Deserialized list should match structure and random links.");

			// Deep copy
			var copyHead = serializer.DeepCopy(node1).GetAwaiter().GetResult();
			Assert.True(CompareHelpers.AreListsEquivalent(node1, copyHead),
				"Deep-copied list should match structure and random links.");

			// Ensure not referencing the exact same objects
			Assert.NotSame(node1, copyHead);
		}

		/// <summary>
		/// 4) Multiple nodes, all Random = null.
		/// Ensures the serializer correctly handles no random references at all.
		/// </summary>
		[Fact]
		public void AllRandomNullTest()
		{
			var serializer = new MySerializer();

			var node1 = new ListNode { Data = "A1" };
			var node2 = new ListNode { Data = "A2" };
			var node3 = new ListNode { Data = "A3" };
			var node4 = new ListNode { Data = "A4" };

			node1.Next = node2; node2.Previous = node1;
			node2.Next = node3; node3.Previous = node2;
			node3.Next = node4; node4.Previous = node3;

			// No random references

			using var ms = new MemoryStream();
			serializer.Serialize(node1, ms).GetAwaiter().GetResult();
			ms.Position = 0;
			var deserialized = serializer.Deserialize(ms).GetAwaiter().GetResult();

			Assert.True(CompareHelpers.AreListsEquivalent(node1, deserialized),
				"All random references are null, structure should match exactly.");

			var copied = serializer.DeepCopy(node1).GetAwaiter().GetResult();
			Assert.True(CompareHelpers.AreListsEquivalent(node1, copied),
				"Deep-copied list should match the original structure with null random references.");
		}

		/// <summary>
		/// 5) Nodes with different Data strings: empty, null, Unicode.
		/// Ensures string serialization/deserialization is handled properly.
		/// </summary>
		[Fact]
		public void NodeWithEmptyAndNullDataTest()
		{
			var serializer = new MySerializer();

			var node1 = new ListNode { Data = "" };
			var node2 = new ListNode { Data = null };
			var node3 = new ListNode { Data = "Some Unicode \u263A" };

			node1.Next = node2; node2.Previous = node1;
			node2.Next = node3; node3.Previous = node2;

			node1.Random = node3;
			node2.Random = null;
			node3.Random = node2;

			using var ms = new MemoryStream();
			serializer.Serialize(node1, ms).GetAwaiter().GetResult();
			ms.Position = 0;
			var deserialized = serializer.Deserialize(ms).GetAwaiter().GetResult();

			Assert.True(CompareHelpers.AreListsEquivalent(node1, deserialized),
				"Deserialized list should match node1 with mixed data (empty, null, Unicode).");

			var copied = serializer.DeepCopy(node1).GetAwaiter().GetResult();
			Assert.True(CompareHelpers.AreListsEquivalent(node1, copied),
				"Deep-copied list should also match.");
		}

		/// <summary>
		/// 6) Repeated serialization/deserialization
		/// to ensure no corruption over multiple cycles.
		/// </summary>
		[Fact]
		public void RepeatedSerializeDeserializeTest()
		{
			var serializer = new MySerializer();
			var head = BuildTestList();

			// 1) First round: Serialize -> Deserialize
			using var ms1 = new MemoryStream();
			serializer.Serialize(head, ms1).GetAwaiter().GetResult();
			ms1.Position = 0;
			var d1 = serializer.Deserialize(ms1).GetAwaiter().GetResult();

			Assert.True(CompareHelpers.AreListsEquivalent(head, d1),
				"First deserialization must match the original.");

			// 2) Second round: Serialize -> Deserialize (based on d1)
			using var ms2 = new MemoryStream();
			serializer.Serialize(d1, ms2).GetAwaiter().GetResult();
			ms2.Position = 0;
			var d2 = serializer.Deserialize(ms2).GetAwaiter().GetResult();

			Assert.True(CompareHelpers.AreListsEquivalent(d1, d2),
				"Second deserialization must match d1 (and thus the original).");
		}

		/// <summary>
		/// Builds a sample list of 3 nodes with Random pointing to different nodes.
		/// Used in the basic test (Test1) and possibly in others.
		/// </summary>
		private ListNode BuildTestList()
		{
			var node1 = new ListNode { Data = "Node 1" };
			var node2 = new ListNode { Data = "Node 2" };
			var node3 = new ListNode { Data = "Node 3" };

			// Link them: 1 <-> 2 <-> 3
			node1.Next = node2;
			node2.Previous = node1;
			node2.Next = node3;
			node3.Previous = node2;

			// Random references
			node1.Random = node3; // 1 -> 3
			node2.Random = node1; // 2 -> 1
			node3.Random = null;  // 3 -> null

			return node1;
		}
	}
}
