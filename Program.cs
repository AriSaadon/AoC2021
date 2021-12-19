using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;

namespace Advent_2021
{
    class Program
    {
        public static List<string> input = File.ReadAllLines("input20.txt").ToList();

        static void Main() => Advent20();

        /// <summary>
        /// 
        /// </summary>
        static void Advent20()
        {

        }

        /// <summary>
        /// I don't like the inline functions.
        /// But I like to keep the solution in one method for the advent if possible.
        /// Spent a lot of time debugging a superficial assumption I made.
        /// </summary>
        static void Advent19()
        {
            List<List<Vector3>> points = new List<List<Vector3>>();
            foreach (string line in input.FindAll(x => x != string.Empty))
            {
                if (line[4] == 's') points.Add(new List<Vector3>());
                else points.Last().Add(Util.ParseVector(line));
            }

            Func<List<Vector3>, HashSet<Vector3>, Vector3> TryTranslationFit = (List<Vector3> non, HashSet<Vector3> al) =>
            {
                foreach (Vector3 p in non)
                {
                    foreach (Vector3 q in al)
                    {
                        Vector3 translation = q - p;
                        List<Vector3> translated = non.Select(x => x + translation).ToList();
                        if (translated.Count(x => al.Contains(x)) >= 12) return translation;
                    }
                }
                return Vector3.Zero;
            };

            List<Vector3> dists = new List<Vector3> { Vector3.Zero };
            HashSet<Vector3> distinctPoints = new HashSet<Vector3>();

            points[0].ForEach(x => distinctPoints.Add(x));
            points.RemoveAt(0);

            Matrix4x4 id = new Matrix4x4(1,0,0,0,  0,1,0,0,  0,0,1,0,  0,0,0,0);
            Matrix4x4 ry = new Matrix4x4(0,0,-1,0,  0,1,0,0,  1,0,0,0,  0,0,0,0);
            Matrix4x4 rx = new Matrix4x4(1,0,0,0,  0,0,-1,0,  0,1,0,0,  0,0,0,0);
            Matrix4x4 rz = new Matrix4x4(0,-1,0,0,  1,0,0,0,  0,0,1,0,  0,0,0,0);
            Matrix4x4[] rots = new Matrix4x4[] { id, rx, rz, rz, rz, rx };

            Action<int> TryAllAlignments = (int i) =>
            {
                for (int m = 0; m < 6; m++)
                {
                    points[i] = points[i].Select(x => Vector3.Transform(x, rots[m])).ToList();
                    for (int n = 0; n < 4; n++)
                    {
                        points[i] = points[i].Select(x => Vector3.Transform(x, ry)).ToList();

                        Vector3 translation = TryTranslationFit(points[i], distinctPoints);
                        if (translation != Vector3.Zero)
                        {
                            dists.Add(translation);
                            points[i].ForEach(x => distinctPoints.Add(x + translation));
                            points.RemoveAt(i);
                            return;
                        }
                    }
                }
            };
            
            while (points.Count > 0) for (int i = 0; i < points.Count; i++) TryAllAlignments(i);
            Console.WriteLine(distinctPoints.Count());

            Func<Vector3, Vector3, int> GetManhattanDist = 
                (x, y) => (int)Math.Abs(x.X - y.X + Math.Abs(x.Y - y.Y) + Math.Abs(x.Z - y.Z));

            int max = int.MinValue;
            dists.ForEach(x => dists.ForEach(y => max = Math.Max(max, GetManhattanDist(x, y))));
            Console.WriteLine(max);
        }

        /// <summary>
        /// Strangely, implementing it didn't take too long.
        /// Sad that I had to write a class.
        /// Could have probably done it with a list of depth, value tuples.
        /// </summary>
        static void Advent18()
        {
            Node18 tree = new Node18(null, input[0]);
            for (int i = 1; i < input.Count; i++)
            {
                tree = new Node18(tree, new Node18(null, input[i]));
                tree.UpdateDepth(0);
                tree.Reduce();
            }
            Console.WriteLine(tree.GetMagnitude());

            int highest = int.MinValue;
            for (int i = 0; i < input.Count; i++)
            {
                for (int j = 0; j < input.Count; j++)
                {
                    if (i == j) continue;
                    tree = new Node18(new Node18(null, input[i]), new Node18(null, input[j]));
                    tree.UpdateDepth(0);
                    tree.Reduce();
                    highest = Math.Max(tree.GetMagnitude(), highest);
                }
            }
            Console.WriteLine(highest);
        }

        public class Node18
        {
            public Node18 parent; //null if root
            public int depth;

            public int value;
            public bool isLeaf;
            public Node18 left, right;

            public Node18(Node18 left, Node18 right)
            {
                isLeaf = false;
                
                this.left = left;
                this.left.parent = this;
                
                this.right = right;
                this.right.parent = this;
            }

            public Node18(Node18 parent, string input)
            {
                this.parent = parent;
                
                int v;
                if (int.TryParse(input, out v))
                {
                    isLeaf = true;
                    value = v;
                }
                else
                {
                    isLeaf = false;
                    input = input.Substring(1, input.Length - 2);

                    int i = 0, depth = 0;
                    while (i < input.Length)
                    {
                        if (input[i] == '[') depth++;
                        if (input[i] == ']') depth--;
                        if (input[i] == ',' && depth == 0 )
                        {
                            left = new Node18(this, input.Substring(0, i));
                            right = new Node18(this, input.Substring(i + 1));
                            break;
                        }

                        i++;
                    }
                }
            }

            public Node18 MoveLeft(Node18 current)
            {
                Node18 x = current;

                bool goUp = true;
                while (x == current || !x.isLeaf)
                {
                    if (goUp)
                    {
                        if (x == x.parent.left)
                        {
                            x = x.parent;
                            if (x.parent == null) return null; //we want to go up on the root.
                        }
                        else if (x == x.parent.right)
                        {
                            x = x.parent.left;
                            goUp = false;
                        }
                    }
                    else x = x.GetRightMost();
                }
                return x;
            }

            public Node18 MoveRight(Node18 current)
            {
                Node18 x = current;

                bool goUp = true;
                while (x == current || !x.isLeaf)
                {
                    if (goUp)
                    {
                        if (x == x.parent.right)
                        {
                            x = x.parent;
                            if (x.parent == null) return null; //we want to go up on the root.
                        }
                        else if (x == x.parent.left)
                        {
                            x = x.parent.right;
                            goUp = false;
                        }
                    }
                    else x = x.GetLeftMost();
                }
                return x;
            }

            public Node18 GetLeftMost()
            {
                Node18 x = this;
                while (!x.isLeaf) x = x.left;
                return x;
            }

            public Node18 GetRightMost()
            {
                Node18 x = this;
                while (!x.isLeaf) x = x.right;
                return x;
            }

            public void UpdateDepth(int currentDepth)
            {
                depth = currentDepth;
                if (!isLeaf)
                {
                    left.UpdateDepth(currentDepth + 1);
                    right.UpdateDepth(currentDepth + 1);
                }
            }

            public void Reduce()
            {
                bool reducable = true;

                while (reducable)
                {
                    reducable = false;

                    Node18 x = GetLeftMost();
                    while (x != null)
                    {
                        if (x.depth > 4)
                        {
                            reducable = true;
                            x.parent.Explode();
                            break;
                        }
                        x = MoveRight(x);
                    }
                    if (reducable) continue;

                    
                    x = GetLeftMost();
                    while (x != null)
                    {
                        if (x.value >= 10)
                        {
                            reducable = true;
                            x.Split();
                            break;
                        }
                        x = MoveRight(x);
                    }
                }
            }

            public void Split()
            {
                int l = value / 2;
                int r = (value / 2.0f) > l ? l + 1 : l;

                left = new Node18(this, l.ToString());
                right = new Node18(this, r.ToString());
                isLeaf = false;
                value = 0;

                UpdateDepth(depth);
            }

            public void Explode()
            {
                Node18 l = MoveLeft(this);
                Node18 r = MoveRight(this);

                if (l != null) l.value += left.value;
                if (r != null) r.value += right.value;

                left = null;
                right = null;
                isLeaf = true;
                value = 0;
            }

            public int GetMagnitude()
            {
                return isLeaf ? value : 3 * left.GetMagnitude() + 2 * right.GetMagnitude();
            }

            public override string ToString()
            {
                return isLeaf ? value.ToString() : $"[{left},{right}]";
            }
        }

        /// <summary>
        /// This one was particularly easy after the previous one.
        /// </summary>
        static void Advent17()
        {
            int left = 287; int right = 309;
            int bot = -76; int top = -48;

            int highest = int.MinValue;
            int sum = 0;
            for (int y = right; y >= bot ; y--)
            {
                for (int x = 0; x <= right; x++)
                {
                    (int, int) pos = (0, 0);
                    (int, int) vel = (x, y);
                    int height = int.MinValue;

                    while (pos.Item1 <= right && pos.Item2 >=  bot) 
                    {
                        pos = (pos.Item1 + vel.Item1, pos.Item2 + vel.Item2);
                        vel = ((vel.Item1 != 0 ? vel.Item1 - 1 : vel.Item1), vel.Item2 - 1);
                        height = Math.Max(height, pos.Item2);

                        if (pos.Item1 >= left && pos.Item1 <= right && pos.Item2 >= bot && pos.Item2 <= top)
                        {
                            sum++;
                            highest = Math.Max(highest, height);
                            break;
                        }
                    }    
                }
            }
            Console.WriteLine(highest);
            Console.WriteLine(sum);
        }

        /// <summary>
        /// I tried to do this first with a FSM, a terrible choice.
        /// Recursion was doable, I take back my statement at day12.
        /// Doing this on a stack would not be pleasant.
        /// Took me a 'long' time to find a certain bug.
        /// </summary>
        static void Advent16()
        {
            string hexadecimals = input[0];
            string bits = string.Empty;
            for (int i = 0; i < hexadecimals.Length; i+= 2)
            {
                int number = int.Parse(hexadecimals.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                bits += Util.InToBS(number);
            }
            Console.WriteLine(Advent16Helper(ref bits));
        }

        public static string GetBits(ref string s, int amount)
        {
            string temp = s.Substring(0, amount);
            s = s.Remove(0, amount);
            return temp;
        }

        static long Advent16Helper(ref string s)
        {
            long version = Util.BSToInT(GetBits(ref s, 3)); long type = Util.BSToInT(GetBits(ref s, 3));

            if (type == 4)
            {
                string lit = string.Empty;
                while (GetBits(ref s, 1) == "1") lit += GetBits(ref s, 4);
                lit += GetBits(ref s, 4);
                
                return Util.BSToInT(lit);
            }
            else
            {
                List<long> subPackets = new List<long>();
                if (GetBits(ref s, 1) == "1") //number
                {
                    long amount = Util.BSToInT(GetBits(ref s, 11));
                    for (int i = 0; i < amount; i++) subPackets.Add(Advent16Helper(ref s));
                }
                else //length
                {
                    long length = Util.BSToInT(GetBits(ref s, 15));
                    int beginLength = s.Length;
                    while (beginLength - s.Length != length) subPackets.Add(Advent16Helper(ref s));
                }

                switch (type)
                {
                    case 0 : return subPackets.Sum();
                    case 1 : return subPackets.Aggregate((long)1, (acc, x) => (acc * x));
                    case 2 : return subPackets.Min();
                    case 3 : return subPackets.Max();
                    case 5 : return subPackets[0] > subPackets[1] ? 1 : 0;
                    case 6 : return subPackets[0] < subPackets[1] ? 1 : 0;
                    default : return subPackets[0] == subPackets[1] ? 1 : 0;
                }
            }
        }

        /// <summary>
        /// Why did it take until .NET 6 to implement a priorityqueue.
        /// Ended up implementing a single source multiple destination shortest path.
        /// Since I forgot how to implement a priority queue, and didn't want to rely on internet.
        /// Early stopping saved me from a very long running time in part 2.
        /// </summary>
        static void Advent15()
        {
            int w = input[0].Length, h = input.Count(); int k = 5;
            Dictionary<int, int> spLengths = new Dictionary<int, int>();
            List<int> map = new List<int>();
            
            for (int y = 0; y < h * k; y++)
            {
                for (int x = 0; x < w * k; x++)
                {
                    int a = y % h; int i = y / h;
                    int b = x % w; int j = x / w;
                    
                    int risk = (int.Parse(input[a][b].ToString()) + (i + j));
                    while (risk > 9) risk -= 9; // not equal to modulo 9
                    map.Add(risk);
                }
            }

            for (int i = 0; i < map.Count; i++) spLengths.Add(i, 1000000);
            spLengths[0] = 0;

            bool done = false;
            while (!done) // early stop if none of the shortest paths have changed.
            {
                done = true;
                for (int j = 0; j < map.Count; j++)
                {
                    int min = Util.GetManhattanNeighbours(j, w * k, h * k).Select(x => spLengths[x] + map[j]).Min();
                    if (min < spLengths[j])
                    {
                        done = false;
                        spLengths[j] = min; // update shortest path with lowest shortest path from a neighbour.
                    }
                }
            }

            Console.WriteLine(spLengths[map.Count - 1]);
        }

        /// <summary>
        /// At first I thought this was a linkedlist question.
        /// A bit heavy on implementation after figuring out a simple way to do it.
        /// Not happy with the code. Was too tired to clean it.
        /// </summary>
        static void Advent14()
        {
            Dictionary<char, long> charCounts = new Dictionary<char, long>();
            Dictionary<(char, char), char> pairFunctions = new Dictionary<(char, char), char>();
            Dictionary<(char, char), long> prevPairs = new Dictionary<(char, char), long>();
            Dictionary<(char, char), long> nextPairs = new Dictionary<(char, char), long>();

            for (int i = 2; i < input.Count; i++)
            {
                string[] tokens = input[i].Split(' ');
                pairFunctions.Add((tokens[0][0], tokens[0][1]), tokens[2][0]);
                prevPairs.Add((tokens[0][0], tokens[0][1]), 0);
                if (!charCounts.ContainsKey(tokens[0][0])) charCounts.Add(tokens[0][0], 0);
            }
            for (int i = 0; i < input[0].Length - 1; i++) prevPairs[(input[0][i], input[0][i + 1])]++;
            
            for (int j = 0; j < 40; j++)
            {
                nextPairs = new Dictionary<(char, char), long>(prevPairs);
                foreach (KeyValuePair<(char, char), long> pair in nextPairs) nextPairs[pair.Key] = 0;

                foreach (KeyValuePair<(char,char), long> pair in prevPairs)
                {
                    char middle = pairFunctions[pair.Key];
                    nextPairs[(pair.Key.Item1, middle)] += pair.Value;
                    nextPairs[(middle, pair.Key.Item2)] += pair.Value;
                }
                prevPairs = nextPairs;
            }

            foreach (KeyValuePair<(char, char), long> pair in prevPairs)
            {
                charCounts[pair.Key.Item1] += pair.Value;
                charCounts[pair.Key.Item2] += pair.Value;
            }

            char first = input[0][0], last = input[0][input[0].Length - 1];
            IEnumerable<long> counts = 
                charCounts.Select(x => (x.Key == first || x.Key == last ? x.Value + 1 : x.Value) >> 1).OrderBy(x => x);
            Console.WriteLine(counts.Last() - counts.First());
        }

        /// <summary>
        /// Spent a lot of time debugging line 34 and 35.
        /// A bit embarassed by it.
        /// Liked the text answer of part 2, made it feel more like a puzzle.
        /// </summary>
        static void Advent13()
        {
            HashSet<(int, int)> points = new HashSet<(int, int)>();
            List<int> folds = new List<int>();
            for (int i = 0, b = 0; i < input.Count; i++)
            {
                if (input[i] == string.Empty) b = 1;
                else if (b == 0) points.Add((input[i].GetInt(',', 0), input[i].GetInt(',', 1)));
                else if (b == 1) folds.Add((input[i].Split(' ')[2][0] == 'x' ? 1 : -1) * input[i].GetInt('=', 1));
            }

            foreach (int fold in folds)
            {
                points = points.Select(x =>
                {
                    int f = Math.Abs(fold);
                    if (fold > 0 && x.Item1 > f) return (f - (x.Item1 - f), x.Item2);
                    else if (fold < 0 && x.Item2 > f) return (x.Item1, f - (x.Item2 - f));
                    else return x;
                }).ToHashSet();
            }
            Console.WriteLine(points.Count);

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 50; x++)
                {
                    Console.Write(points.Contains((x, y)) ? "#" : ".");
                }
                Console.Write("\n");
            }
        }

        /// <summary>
        /// I liked this one as well. It seems I just like working with stacks.
        /// And also with graphs.
        /// From now on I will try to stop using recursion and will try to instead use a stack.
        /// </summary>
        static void Advent12()
        {
            Dictionary<string, HashSet<string>> graph = new Dictionary<string, HashSet<string>>();
            foreach (string line in input) 
            {
                string[] edge = line.Split('-');
                if (!graph.ContainsKey(edge[0])) graph.Add(edge[0], new HashSet<string>());
                if (!graph.ContainsKey(edge[1])) graph.Add(edge[1], new HashSet<string>());
                graph[edge[0]].Add(edge[1]);
                graph[edge[1]].Add(edge[0]);
            }

            Stack<List<string>> st = new Stack<List<string>>() {};
            st.Push(new List<string>() { "start" });

            int amount = 0;
            while (st.Count > 0)
            {
                List<string> path = st.Pop();
                foreach (string next in graph[path.Last()])
                {
                    if (next == "end") amount++;
                    else if (next == "start") continue;
                    else if (char.IsUpper(next[0]) || !path.Contains(next)) st.Push(new List<string>(path) { next });
                    else if (path.Contains(next) && !path.Contains("double")) //remove for part 1
                        st.Push(new List<string>(path.Select(x => x == next ? "double" : x)) { next });
                }
            }
            Console.WriteLine(amount);
        }

        /// <summary>
        /// Why such a small input size?
        /// Is it difficult to reverse engineer input that converges to synchronization in achievable time?
        /// </summary>
        static void Advent11()
        {
            int width = input[0].Length, height = input.Count();
            List<int> cavern = new List<int>();
            input.ForEach(x => Array.ForEach(x.ToCharArray(), c => cavern.Add(int.Parse(c.ToString()))));
            
            for (int flashCount = 0, loop = 0; loop < 345; loop++) //make while loop obv. instead of 345 times.
            {
                for (int i = 0; i < cavern.Count; i++) Advent11Helper(cavern, width, height, i);
                int temp = flashCount;
                flashCount += cavern.Count(x => x < 0);
                cavern = cavern.Select(x => x < 0 ? 0 : x).ToList(); //LINQ really shouldn't be used for this...

                if (loop == 99) Console.WriteLine(flashCount);
                if (flashCount - temp == cavern.Count) Console.WriteLine(loop + 1);
            }
        }

        static void Advent11Helper(List<int> cavern, int width, int height, int i)
        {
            cavern[i]++;
            if (cavern[i] > 9)
            {
                cavern[i] = int.MinValue; //prevent a squid from flashing more than once in an iteration.
                Util.GetNeighbours(i, width, height).ForEach(n => Advent11Helper(cavern, width, height, n));
            }
        }

        /// <summary>
        /// I liked this one, easy but fun.
        /// </summary>
        static void Advent10()
        {
            List<char> openers = new List<char> { '(', '[', '{', '<' };
            List<char> closers = new List<char> { ')', ']', '}', '>' };
            List<long> completeScores = new List<long>();
            Stack<char> st = new Stack<char>();

            int part1 = 0;
            input.ForEach(line =>
            {
                bool isCorrupt = false;
                for (int i = 0; i < line.Length; i++)
                {
                    if (openers.Contains(line[i])) st.Push(line[i]);
                    else if (openers.IndexOf(st.Pop()) != closers.IndexOf(line[i]))
                    {
                        part1 += new int[] { 3, 57, 1197, 25137 }[closers.IndexOf(line[i])];
                        st.Clear();
                        isCorrupt = true;
                        break;
                    }
                }

                if (!isCorrupt)
                {
                    long score = 0;
                    while (st.Count > 0) score = (score * 5) + openers.IndexOf(st.Pop()) + 1;
                    completeScores.Add(score);
                }
            });
            Console.WriteLine(part1);
            Console.WriteLine(completeScores.OrderBy(x => x).ToList()[completeScores.Count >> 1]);
        }

        /// <summary>
        /// Was quite tired with this one, really dont like the code.
        /// Would have rather modelled the heightMap as a 1d array.
        /// </summary>
        static void Advent9()
        {
            List<List<int>> hMap = input.Select(line =>
            {
                List<int> row = new List<int>();
                for (int i = 0; i < line.Length; i++) row.Add(int.Parse(line[i].ToString()));
                return row;
            }).ToList();

            Console.WriteLine(Enumerable.Range(0, hMap.Count).Sum(i => Enumerable.Range(0, hMap[0].Count).Sum(j =>
            {
                List<int> neighbours = new List<int>();
                if (i > 0) neighbours.Add(hMap[i - 1][j]);
                if (i < hMap.Count - 1) neighbours.Add(hMap[i + 1][j]);
                if (j > 0) neighbours.Add(hMap[i][j - 1]);
                if (j < hMap[0].Count - 1) neighbours.Add(hMap[i][j + 1]);
                return neighbours.All(x => hMap[i][j] < x) ? hMap[i][j] + 1 : 0;
            })));

            List<int> basinSizes = new List<int>();
            for (int i = 0; i < hMap.Count; i++) for (int j = 0; j < hMap[0].Count; j++)
            {
                if (hMap[i][j] < 9) basinSizes.Add(Advent9Helper(hMap, i, j));
            }
            basinSizes = basinSizes.OrderByDescending(x => x).ToList();
            Console.WriteLine(basinSizes[0] * basinSizes[1] * basinSizes[2]);
        }

        static int Advent9Helper(List<List<int>> hMap, int i, int j)
        {
            int sum = 0;
            hMap[i][j] = 9; //Set tag so we stop exploring.
            
            if (i > 0 && hMap[i - 1][j] < 9) sum += Advent9Helper(hMap, i - 1, j);
            if (i < hMap.Count - 1 && hMap[i + 1][j] < 9) sum += Advent9Helper(hMap, i + 1, j);
            if (j > 0 && hMap[i][j - 1] < 9) sum += Advent9Helper(hMap, i, j - 1);
            if (j < hMap[0].Count - 1 && hMap[i][j + 1] < 9) sum += Advent9Helper(hMap, i, j + 1);

            return 1 + sum;
        }

        /// <summary>
        /// Though I could do it also with deduction, I ended up brute forcing it since the amount of permutations are 7!.
        /// a GetPermutations() and GetCombinations() function in LINQ would be nice.
        /// </summary>
        static void Advent8()
        {
            IEnumerable<IEnumerable<string>> splittedInput = input.Select(x => x.Split(' '));
            List<List<string>> signals = splittedInput.Select(x => x.Take(10).ToList()).ToList();
            List<List<string>> outputs = splittedInput.Select(x => x.Skip(x.Count() - 4).ToList()).ToList();
            //List<int> ezDigits = new List<int> { 2, 3, 4, 7};
            //Console.WriteLine(outputs.Sum(x => x.Sum(y => ezDigits.Contains(y.Length) ? 1 : 0)));

            List<List<char>> perms = new List<List<char>>();
            "abcdefg".ToList().GetPermutations(new List<char>(), perms);            
            List<int> validConfigs = new List<int>{ 119, 36, 93, 109, 46, 107, 123, 37, 127, 111 };

            Func<string, List<char>, int> GetConfig = (string signal, List<char> code) =>
                Enumerable.Range(0, signal.Length).Sum(i => 1 << code.IndexOf(signal[i]));

            Console.WriteLine(Enumerable.Range(0, input.Count).Sum(i =>
            {
                List<string> signal = signals[i];
                List<string> output = outputs[i];

                string answer = string.Empty;
                List<char> key = perms.Find(c => signal.All(s => validConfigs.Contains(GetConfig(s, c))));
                output.ForEach(s => answer += validConfigs.IndexOf(GetConfig(s, key)).ToString());
                return int.Parse(answer);
            }));
        }

        /// <summary>
        /// Ended up brute forcing this one, even though I did part 1 with the median.
        /// I still don't understand for what type of distance functions the mean will give the answer for part 2
        /// Surely there are distance functions where using the mean is not correct.
        /// </summary>
        static void Advent7()
        {
            List<int> crabs = input[0].Split(',').Select(x => int.Parse(x)).ToList();
            /*List<int> distTable = new int[max + 1].ToList();
            for (int i = 0, acc = 0; i < distTable.Count; i++)
            {
                distTable[i] = i + acc; //remove additional acc for part 1
                acc += i;
            }*/

            int bestDistance = int.MaxValue;
            for (int i = 0; i < crabs.Max(); i++)
            {
                //int distance = crabs.Sum(x => Enumerable.Range(0, Math.Abs(i - x) + 1).Sum());
                int distance = crabs.Sum(x => Math.Abs(i - x));
                bestDistance = distance < bestDistance ? distance : bestDistance;
            }
            Console.WriteLine(bestDistance);
        }

        /// <summary>
        /// Instead of seeing the simple solution, working with counts, I ended up making a DP table.
        /// Did not have my day, considering how simple the challenge was.
        /// </summary>
        static void Advent6()
        {
            List<int> fish = input[0].Split(',').Select(x => int.Parse(x)).ToList();
            Dictionary<(int, int), long> dpTable = new Dictionary<(int, int), long>();

            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (i == 0 && j == 0) dpTable[(i, j)] = 1;
                    else if (i == 0) dpTable[(i, j)] = 0;
                    else if (j == 0) dpTable[(i, j)] = 1 + dpTable[(i - 1, 6)] + dpTable[(i - 1, 8)];
                    else dpTable[(i, j)] = dpTable[(i - 1, j - 1)];
                }
            }
            Console.WriteLine(fish.Sum(x => dpTable[(255, x)]) + fish.Count);
        }

        /// <summary>
        /// Started to relax the small line count fixation from here on out.
        /// It should still be short, but lines should be readable.
        /// Though I work really zoomed out, I like a max line lenght of about 120 char.
        /// </summary>
        static void Advent5()
        {
            Dictionary<(int, int), int> points = new Dictionary<(int, int), int>();
            input.ForEach(x =>
            {
                int[] numbers = Regex.Matches(x, @"\d+").Select(y => int.Parse(y.Value)).ToArray();
                (int, int) a = (numbers[0], numbers[1]), b = (numbers[2], numbers[3]);

                bool isH = a.Item1 == b.Item1; bool isV = a.Item2 == b.Item2; bool isPart1 = false; //true for part1 - false for part 2
                if (isPart1 ? isH ^ isV : true)
                {
                    points[a] = points.GetValueOrDefault(a) + 1;
                    while (a != b)
                    {
                        a = (a.Item1 + (isH ? 0 : Math.Sign(b.Item1 - a.Item1)), a.Item2 + (isV ? 0 : Math.Sign(b.Item2 - a.Item2)));
                        points[a] = points.GetValueOrDefault(a) + 1;
                    }
                }
            });
            Console.WriteLine(points.Values.Count(x => x > 1));
        }

        /// <summary>
        /// Just like day3 I tried to keep the amount of lines to a reasonable minimum.
        /// Some lines became disguisting because of it.
        /// </summary>
        static void Advent4()
        {
            List<int> draws = input[0].Split(',').Select(x => int.Parse(x)).ToList();
            input.RemoveAt(0);

            List<List<int>> grids = new List<List<int>>(); int answer = int.MinValue;
            Enumerable.Range(0, input.RemoveAll(x => x == string.Empty)).ToList().ForEach(x => grids.Add(new List<int>()));

            int size = input[0].Count(x => x == ' ');
            for (int i = 0; i < input.Count; i++) grids[i / size].AddRange(input[i].Split(' ').ToList().FindAll(x => x != string.Empty).Select(x => int.Parse(x)));

            Func<List<int>, bool> hasNiceRow = (g) => Enumerable.Range(0, size).ToList().Any(rowID => g.Where((item, index) => index >= rowID * size && index < (rowID + 1) * size).All(x => x == 0));
            Func<List<int>, bool> hasNiceCol = (g) => Enumerable.Range(0, size).ToList().Any(colID => g.Where((item, index) => index % size == colID).All(x => x == 0));

            //for (int i = 0; answer == int.MinValue; i++) for (int j = 0; j < grids.Count; j++) // Loop for part 1
            //{
            //    grids[j] = grids[j].Select(x => x == draws[i] ? 0 : x).ToList();
            //    if (checkRows(grids[j]) || checkColumns(grids[j])) answer = answer < grids[j].Sum() * draws[i] ? grids[j].Sum() * draws[i] : answer;
            //}
            for (int i = 0; i < draws.Count; i++) for (int j = 0; j < grids.Count; j++) //Loop for part 2
            {
                grids[j] = grids[j].Select(x => x == draws[i] ? 0 : x).ToList();
                if (hasNiceRow(grids[j]) || hasNiceCol(grids[j])) answer = grids[j].Sum() * draws[i];
                if (hasNiceRow(grids[j]) || hasNiceCol(grids[j])) grids.RemoveAt(j--);
            }
            Console.WriteLine(answer);
        }

        /// <summary>
        /// This was the first puzzle that I did for the aoc 2021.
        /// Wanted to keep the number of lines as small as possible, even though individual lines became unreadable.
        /// </summary>
        static void Advent3()
        {
            int gamma = 0; int[] bitCounts = new int[input[0].Length];
            input.ForEach(l => Enumerable.Range(0, bitCounts.Length).ToList().ForEach(i => bitCounts[i] += l[i] == '1' ? 1 : -1));
            Enumerable.Range(0, bitCounts.Length).ToList().ForEach(i => gamma += bitCounts[i] > 0 ? (1 << (bitCounts.Length  -1 - i)) : 0);
            Console.WriteLine((gamma ^ ((1 << bitCounts.Length) - 1)) * gamma);

            int i = 0;
            while (input.Count > 1)
            {
                int bitCount = 0;
                input.ForEach(l => bitCount += l[i] == '1' ? 1 : -1);
                input = input.FindAll(l => l[i] == (bitCount >= 0 ? '0' : '1')); //Change 0 and 1 around depending on oxygen or CO2
                i++;
            }

            int C02xgen = 0;
            Enumerable.Range(0, input[0].Length).ToList().ForEach(i => C02xgen += (input[0][i] == '1') ? (1 << (input[0].Length - 1 - i)) : 0);
            Console.WriteLine(C02xgen);
        }

        /// <summary>
        /// Dit this one on day 5 as well. Again wanted to one-line.
        /// Didn't work out for part 2.
        /// </summary>
        static void Advent2()
        {
            Console.WriteLine(input.FindAll(x => x[0] != 'f').Sum(x => (x.Split(' ')[0][0] == 'u' ? -int.Parse(x.Split(' ')[1]) : int.Parse(x.Split(' ')[1]))) * input.FindAll(x => x[0] == 'f').Sum(x => (int.Parse(x.Split(' ')[1]))));

            (int, int) pos = (0, 0); int aim = 0;
            input.ForEach(x =>
            {
                if (x[0] == 'f') pos = (pos.Item1 + int.Parse(x.Split(' ')[1]), pos.Item2 + int.Parse(x.Split(' ')[1]) * aim);
                else if (x[0] == 'd') aim += int.Parse(x.Split(' ')[1]);
                else if (x[0] == 'u') aim -= int.Parse(x.Split(' ')[1]);
            });
            Console.WriteLine(pos.Item1 * pos.Item2);
        }

        /// <summary>
        /// Did this one on day 5 I think. Wanted to one-line.
        /// </summary>
        static void Advent1()
        {
            Console.WriteLine(Enumerable.Range(1, input.Count - 1).ToList().Count(i => int.Parse(input[i]) > int.Parse(input[i-1])));
            Console.WriteLine(Enumerable.Range(3, input.Count - 3).ToList().Count(i => (int.Parse(input[i - 3]) + int.Parse(input[i - 2]) + int.Parse(input[i - 1])) < (int.Parse(input[i - 2]) + int.Parse(input[i - 1]) + int.Parse(input[i]))));
        }
    }

    public static class Util
    {
        public static int GetInt(this string s, char c, int i)
        {
            return int.Parse(s.Split(c)[i]);
        }

        public static List<List<TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, int, TKey> keySelector)
        {
            return source.Select((item, index) => (item, index)).GroupBy(x => keySelector(x.item, x.index)).Select(x => x.Select(y => y.item).ToList()).ToList();
        }

        public static string InToBS(int i)
        {
            int x = i;
            string s = string.Empty;
            
            while(x > 0)
            {
                s = s.Insert(0, (x % 2).ToString());
                x = x >> 1;
            }

            while(s.Length < 8)
            {
                s = s.Insert(0, "0");
            }

            return s;
        }
        
        public static Vector3 ParseVector(string s)
        {
            float[] floats = s.Split(',').Select(f => float.Parse(f)).ToArray();
            return new Vector3(floats[0], floats[1], floats[2]);
        }

        public static long BSToInT(string bs)
        {
            string s = bs;
            long x = 0;

            while (s != string.Empty)
            {
                x = (x << 1) + (s.First() == '1' ? 1 : 0);
                s = s.Substring(1);
            }
            return x;
        }

        public static List<int> GetNeighbours(int x, int w, int h)
        {
            List<int> neighbours;

            //corners
            if (x == 0) neighbours = new List<int> { x + 1, x + w, x + w + 1 };
            else if (x == w - 1) neighbours = new List<int> { x - 1, x + w - 1, x + w };
            else if (x == w * (h - 1)) neighbours = new List<int> { x - w, x - w + 1, x + 1 };
            else if (x == (w * h) - 1) neighbours = new List<int> { x - w - 1, x - w, x - 1 };

            // edges
            else if (x % w == 0) neighbours = new List<int> { x - w, x - w + 1, x + 1, x + w, x + w + 1 };
            else if (x % w == w - 1) neighbours = new List<int> { x - w - 1, x - w, x - 1, x + w - 1, x + w };
            else if (x < w) neighbours = new List<int> { x - 1, x + 1, x + w - 1, x + w, x + w + 1 };
            else if (x >= w * (h - 1)) neighbours = new List<int> { x - w - 1, x - w, x - w + 1, x - 1, x + 1 };

            // bulk
            else neighbours = new List<int> { x - w - 1, x - w, x - w + 1, x - 1, x + 1, x + w - 1, x + w, x + w + 1 };

            return neighbours;
        }

        public static int[] GetManhattanNeighbours(int x, int w, int h)
        {
            bool left = x % w == 0;
            bool right = x % w == w - 1;
            bool up = x < w;
            bool down = x >= w * (h - 1);

            if (!left && !right && !up && !down) return new int[] { x + w, x - 1, x - w, x + 1 };

            else if (!down && !right && !left) return new int[] { x + w, x - 1, x + 1 };
            else if (!up && !right && !left) return new int[] { x - w, x - 1, x + 1 };
            else if (!up && !down && !left) return new int[] { x - w, x + w, x - 1 };
            else if (!up && !down && !right) return new int[] { x - w, x + w, x + 1 };

            else if (x == 0) return new int[] { x + w, x + 1 };
            else if (x == w-1) return new int[] { x + w, x - 1 };
            else if (x == w * (h-1)) return new int[] { x - w, x + 1 };
            else return new int[] { x - w, x - 1 };
        }

        public static void GetPermutations<TSource>(this List<TSource> source, List<TSource> acc, List<List<TSource>> output)
        {
            if (source.Count() == 1)
            {
                acc.Add(source.First());
                output.Add(acc);
            }
            else
            {
                source.ForEach(x =>
                {
                    List<TSource> newAcc = new List<TSource>();
                    newAcc.AddRange(acc);
                    newAcc.Add(x);

                    List<TSource> newSource = new List<TSource>();
                    newSource.AddRange(source);
                    newSource.Remove(x);
                    newSource.GetPermutations(newAcc, output);
                });
            }
        }
    }
}