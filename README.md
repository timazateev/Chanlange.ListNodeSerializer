# Chanlange.ListNodeSerializer

This repository demonstrates a custom serializer for a doubly-linked list (`ListNode`), which includes:
- `Previous`, `Next` references (forming a doubly-linked chain).
- A `Random` reference that may point to any node within the list (or be `null`).

We provide:
1. **`IListSerializer` interface** – defines methods `Serialize`, `Deserialize`, and `DeepCopy`.
2. **`MySerializer` (implementation)** – writes and reads each node in a custom binary format:
   - Writes/reads the node's `Data` (string) with `-1` marking `null`.
   - Writes/reads the node's `Random` reference as an integer index (`-1` if `null`).

We also include **tests** (with xUnit) covering various scenarios: empty lists, single node, random references pointing to different nodes, `Data` as `null` or `""`, repeated serialization, etc.

---

## How to Build and Run

1. **Clone the repository** (or download the source).
2. **Open in Visual Studio** (or your preferred .NET IDE).
3. **Restore NuGet packages** (if any are missing).
4. **Build** the solution (e.g. `Ctrl+Shift+B` in Visual Studio).

### Console Application for Large Files

We have a console project (e.g. `Chanlange.ListNodeSerializer`) which:
- Reads a **large file** where each line is turned into a `ListNode`.
- Creates a doubly-linked list of all those nodes.
- Optionally sets random references.
- **Serializes** the entire list into a `MemoryStream`.
- **Deserializes** back into a new list.
- Performs a **DeepCopy**.
- Prints out timings and list counts at each stage.

**Usage**:
1. **Navigate** to the output folder (e.g. `bin\Debug\net6.0`).
2. Run the console exe:
   ```powershell
   .\Chanlange.ListNodeSerializer.exe

```
Please enter the path to the big test data file:
C:\Users\timaz\source\repos\Chanlange.ListNodeSerializer\Files\testLargeFile.txt

Reading file from: C:\Users\timaz\source\repos\Chanlange.ListNodeSerializer\Files\testLargeFile.txt
Finished reading. Total nodes: 2423216

Serialization completed in 4.5909536 seconds.
Serialized data size: 124336667 bytes.

Deserialization completed in 6.683648 seconds.
Deserialized list node count: 2423216

DeepCopy completed in 2.3190647 seconds.
Copied list node count: 2423216

All done. Press any key to exit.
```
