using Chanlange.ListNodeSerializer.Nodes;

namespace Chanlange.ListNodeSerializer.Test
{
	public static class CompareHelpers
	{
		/// <summary>
		/// Checks if two doubly-linked lists with Random pointers are equivalent by structure and data.
		/// </summary>
		public static bool AreListsEquivalent(ListNode headA, ListNode headB)
		{
			var listA = GetNodesArray(headA);
			var listB = GetNodesArray(headB);

			if (listA.Length != listB.Length) return false;

			for (int i = 0; i < listA.Length; i++)
			{
				if (listA[i].Data != listB[i].Data) return false;

				int randomIndexA = GetIndex(listA, listA[i].Random);
				int randomIndexB = GetIndex(listB, listB[i].Random);

				if (randomIndexA != randomIndexB) return false;
			}

			return true;
		}

		private static ListNode[] GetNodesArray(ListNode head)
		{
			List<ListNode> list = new List<ListNode>();
			var current = head;
			while (current != null)
			{
				list.Add(current);
				current = current.Next;
			}
			return list.ToArray();
		}

		private static int GetIndex(ListNode[] array, ListNode node)
		{
			if (node == null) return -1;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == node) return i;
			}
			return -1;
		}
	}
}
