﻿using Sprache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sprache
{
    static class Extensions
    {
        // 行コメントを消化
        public static readonly Parser<string> SingleLineComment =
            from first in Parse.String("//")
            from rest in Parse.AnyChar.Except(Parse.Char((char)13)).Many().Text()
            select rest;

        // ホワイトスペースに加えて行コメントを消化するToken
        public static Parser<T> TokenWithSkipComment<T>(this Parser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException("parser");

            return from leading in SingleLineComment.Or(Parse.WhiteSpace.Return("")).Many()
                   from item in parser
                   from trailing in SingleLineComment.Or(Parse.WhiteSpace.Return("")).Many()
                   select item;
        }
    }
}

namespace VpdSample
{
    #region Vpd
    struct Vector3
    {
        public Single X;
        public Single Y;
        public Single Z;

        public Vector3(Single x, Single y, Single z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}]", X, Y, Z);
        }
    }

    struct Quaternion
    {
        public Single X;
        public Single Y;
        public Single Z;
        public Single W;

        public Quaternion(Single x, Single y, Single z, Single w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override string ToString()
        {
            return String.Format("({0}, {1}, {2}, {3})", X, Y, Z, W);
        }
    }

    class Node
    {
        public String Name { get; set; }
        public Vector3 Translation { get; set; }
        public Quaternion Rotation { get; set; }

        public override string ToString()
        {
            return String.Format("<{0}: pos{1}, rot{2}>", Name, Translation, Rotation);
        }
    }
    #endregion

    static class VpdParse
    {
        /// <summary>
        /// ホワイトスペース以外を消化 
        /// </summary>
        static readonly Parser<char> NotWhiteSpace = Parse.Char(x => !char.IsWhiteSpace(x), "whitespace");

        /// <summary>
        /// 符号付浮動小数
        /// </summary>
        static readonly Parser<Single> SignedFloat =
            from negative in Parse.Char('-').Optional().Select(x => x.IsDefined ? "-" : "")
            from num in Parse.Decimal
            select Convert.ToSingle(negative + num);

        /// <summary>
        /// Vocaloid Pose Data file
        ///
        /// miku.osm;		// 親ファイル名
        /// 14;				// 総ポーズボーン数
        /// </summary>
        static Parser<Int32> Header
        {
            get
            {
                return
                    from _signature in Parse.String("Vocaloid Pose Data file")
                    from _osm in Parse.String("miku.osm;").TokenWithSkipComment()
                    from n in (
                        from number in Parse.Number.Select(x => Convert.ToInt32(x))
                        from semicolon in Parse.Char(';')
                        select number
                    ).TokenWithSkipComment()
                    select n;
            }
        }

        /// <summary>
        /// Bone0{右親指１
        ///  -0.000000,0.000000,0.000000;				// trans x,y,z
        ///  0.071834,0.539167,0.266196,0.795784;		// Quatanion x,y,z,w
        /// }
        /// </summary>
        static Parser<Node> Node
        {
            get
            {
                return
                    from bone in Parse.String("Bone").Text()
                    from index in Parse.Number
                    from open in Parse.Char('{')
                    from name in NotWhiteSpace.AtLeastOnce().Text()

                    from translation in (
                        from x in SignedFloat.Select(x => Convert.ToSingle(x))
                        from _c0 in Parse.Char(',')
                        from y in SignedFloat.Select(x => Convert.ToSingle(x))
                        from _c1 in Parse.Char(',')
                        from z in SignedFloat.Select(x => Convert.ToSingle(x))
                        from _sc in Parse.Char(';')
                        select new Vector3(x, y, z)
                    ).TokenWithSkipComment()

                    from rotation in (
                        from x in SignedFloat.Select(x => Convert.ToSingle(x))
                        from _c0 in Parse.Char(',')
                        from y in SignedFloat.Select(x => Convert.ToSingle(x))
                        from _c1 in Parse.Char(',')
                        from z in SignedFloat.Select(x => Convert.ToSingle(x))
                        from _c2 in Parse.Char(',')
                        from w in SignedFloat.Select(x => Convert.ToSingle(x))
                        from _sc in Parse.Char(';')
                        select new Quaternion(x, y, z, w)
                    ).TokenWithSkipComment()

                    from close in Parse.Char('}')
                    select new Node
                    {
                        Name=name,
                        Translation=translation,
                        Rotation=rotation,
                    };
            }
        }

        public static IEnumerable<Node> Execute(String text)
        {
            var parser =
                from boneCount in Header
                from nodes in Node.Token().Repeat(boneCount)
                select nodes;
            ;

            return parser.Parse(text);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var path = args.First();
            var text = File.ReadAllText(path, Encoding.GetEncoding(932));

            var result = VpdParse.Execute(text);

            foreach (var item in result)
            {
                Console.WriteLine(item);
            }
        }
    }
}
