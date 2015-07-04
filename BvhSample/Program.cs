using Sprache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BvhSample
{
    public struct Vector3
    {
        public Single X;
        public Single Y;
        public Single Z;
    }

    public enum ChannelType
    {
        Xposition,
        Yposition,
        Zposition,
        Zrotation,
        Yrotation,
        Xrotation,
    }

    class Node
    {
        public String Name { get; private set; }
        public Vector3 Offset { get; private set; }
        public ChannelType[] Channels { get; private set; }
        public List<Node> Children { get; private set; }

        public Node(String name, Vector3 offset, IEnumerable<ChannelType> channels = null, IEnumerable<Node> children = null)
        {
            Name = name;
            Offset = offset;
            Channels = channels != null ? channels.ToArray() : new ChannelType[] { };
            Children = children != null ? children.ToList() : new List<Node>();
        }

        public override string ToString()
        {
            return String.Format("{0}[{1}, {2}, {3}]{4}", Name, Offset.X, Offset.Y, Offset.Z, String.Join("", Channels));
        }

        public void Traverse(Action<Node, int> pred, int level = 0)
        {
            pred(this, level);

            foreach (var child in Children)
            {
                child.Traverse(pred, level + 1);
            }
        }
    }

    static class BvhParser
    {
        public static Parser<Node> Parser
        {
            get
            {
                return from herarchy in Parse.String("HIERARCHY")
                       select new Node("dummy", new Vector3());

            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var path = args.First();
            var bvhtext=File.ReadAllText(path);

            var parser = BvhParser.Parser;
            var root = parser.Parse(bvhtext);

            root.Traverse((node, level) => {
                Console.WriteLine(String.Format("{0}{1}"
                    , String.Join("", Enumerable.Repeat("  ", level).ToArray()) // indent
                    , node
                    ));
            });
        }
    }
}
