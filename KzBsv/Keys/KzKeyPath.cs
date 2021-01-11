#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace KzBsv {

	/// <summary>
	/// Represent a BIP32 style key path.
	/// </summary>
	public class KzKeyPath
	{
        /// <summary>
        /// True if the path starts with m.
        /// False if the path starts with M.
        /// null if the path starts with an index.
        /// </summary>
        public bool? FromPriv { get; private set; } = null;

        /// <summary>
        /// Path indices, in order.
        /// Hardened indices have the 0x80000000u bit set.
        /// </summary>
        public UInt32[] Indices { get; private set; } = new UInt32[0];

		public UInt32 this[int index] => Indices[index];

        /// <summary>
        /// How many numeric Indices there are.
        /// </summary>
        public int Count => Indices.Length;

        /// <summary>
        /// HardenedBit is 0x80000000u.
        /// </summary>
        public const UInt32 HardenedBit = 0x80000000u;

        /// <summary>
        /// Creates an empty path (zero indices) with FromPriv set to null.
        /// </summary>
		public KzKeyPath()
		{
		}

		static UInt32 ParseIndex(string i)
		{
			var hardened = i.Length > 0 && i[^1] == '\'' || i[^1] == 'H';
			var index = UInt32.Parse(hardened ? i[..^1] : i);
            if (index >= HardenedBit)
                throw new ArgumentException($"Indices must be less than {HardenedBit}.");
			return hardened ? index | HardenedBit : index;
		}

        static UInt32[] ParseIndices(string path)
        {
			return path.Split('/').Where(p => p != "m" && p != "M" && p != "").Select(ParseIndex).ToArray();
        }

        /// <summary>
        /// Returns a sequence of KzKeyPaths from comma separated string of paths.
        /// </summary>
        /// <param name="v">Comma separated string of paths.</param>
        /// <returns></returns>
        public static IEnumerable<KzKeyPath> AsEnumerable(string v) {
            foreach (var kp in v.Split(','))
                yield return new KzKeyPath(kp);
        }

        /// <summary>
        /// Parse a KzHDKeyPath
        /// </summary>
        /// <param name="path">The KzHDKeyPath formated like a/b/c'/d. Appostrophe indicates hardened/private. a,b,c,d must convert to 0..2^31.
        /// Optionally the path can start with "m/" for private extended master key derivations or "M/" for public extended master key derivations.
        /// </param>
        /// <returns></returns>
        public static KzKeyPath Parse(string path)
		{
			return new KzKeyPath(path);
		}

		/// <summary>
		/// Creates a path based on its formatted string representation.
		/// </summary>
		/// <param name="path">The KzHDKeyPath formated like a/b/c'/d. Appostrophe indicates hardened/private. a,b,c,d must convert to 0..2^31.
        /// Optionally the path can start with "m/" for private extended master key derivations or "M/" for public extended master key derivations.
        /// </param>
		/// <returns></returns>
		public KzKeyPath(string path)
		{
            FromPriv = path.StartsWith('m') ? true : path.StartsWith('M') ? false : (bool?)null;
            Indices = ParseIndices(path);
		}

        /// <summary>
        /// Creates a path with the properties provided.
        /// FromPriv is set to null.
        /// </summary>
        /// <param name="indices">Sets the indices. Hardened indices must have the HardenedBit set.</param>
		public KzKeyPath(params UInt32[] indices)
		{
            FromPriv = null;
			Indices = indices;
		}

        /// <summary>
        /// Creates a path with the properties provided.
        /// </summary>
        /// <param name="fromPriv">Sets FromPriv if provided.</param>
        /// <param name="indices">Sets the indices. Hardened indices must have the HardenedBit set.</param>
		public KzKeyPath(bool? fromPriv, params UInt32[] indices)
		{
            FromPriv = fromPriv;
			Indices = indices;
		}

        /// <summary>
        /// Extends path with additional indices.
        /// FromPriv of additionalIndices is ignored.
        /// </summary>
        /// <param name="additionalIndices"></param>
        /// <returns>New path with concatenated indices.</returns>
		public KzKeyPath Derive(KzKeyPath additionalIndices)
		{
			return new KzKeyPath(FromPriv, Indices.Concat(additionalIndices.Indices).ToArray());
		}

        /// <summary>
        /// Extends path with additional index.
        /// </summary>
        /// <param name="index">Values with HardenedBit set are hardened.</param>
        /// <returns>New path with concatenated index.</returns>
		public KzKeyPath Derive(UInt32 index)
		{
            return new KzKeyPath(FromPriv, Indices.Concat(new[] { index }).ToArray());
		}

        /// <summary>
        /// Extends path with additional index.
        /// </summary>
        /// <param name="index">Value must be non-negative and less than HardenedBit (which an int always is...)</param>
        /// <param name="hardened">If true, HardenedBit will be added to index.</param>
        /// <returns>New path with concatenated index.</returns>
		public KzKeyPath Derive(int index, bool hardened)
		{
			if (index < 0) throw new ArgumentOutOfRangeException("index", "Must be non-negative.");
			var i = (UInt32)index;
            return Derive(hardened ? i | HardenedBit : i);
		}

        /// <summary>
        /// Extends path with additional indices from string formatted path.
        /// Any "m/" or "M/" prefix in path will be ignored.
        /// </summary>
        /// <param name="path">The indices in path will be concatenated.</param>
        /// <returns>New path with concatenated indices.</returns>
		public KzKeyPath Derive(string path)
		{
			return Derive(new KzKeyPath(path));
		}

        /// <summary>
        /// Returns a new path with one less index, or null if path has no indices.
        /// </summary>
		public KzKeyPath Parent {
            get {
                if (Count == 0) return null;
                return new KzKeyPath(FromPriv, Indices.Take(Indices.Length - 1).ToArray());
            }
        }

        /// <summary>
        /// Returns a new path with the last index incremented by one.
        /// Throws InvalidOperation if path contains no indices.
        /// </summary>
        /// <returns>Returns a new path with the last index incremented by one.</returns>
        public KzKeyPath Increment()
        {
            if (Count == 0) throw new InvalidOperationException();
            var indices = Indices.ToArray();
            indices[Count - 1]++;
            return new KzKeyPath(FromPriv, indices);
        }

		public override string ToString()
		{
            var sb = new StringBuilder();
            if (FromPriv == true) sb.Append("m/");
            else if (FromPriv == false) sb.Append("M/");
            foreach (var i in Indices) {
                sb.Append(i & ~HardenedBit);
                if (i >= HardenedBit) sb.Append("'");
                sb.Append("/");
            }
            sb.Length--;
            return sb.ToString();
		}

		public override int GetHashCode() => ToString().GetHashCode();
        public bool Equals(KzKeyPath o) => (object)o != null && ToString().Equals(o.ToString());
        public override bool Equals(object obj) => obj is KzKeyPath && this == (KzKeyPath)obj;
        public static bool operator ==(KzKeyPath x, KzKeyPath y) => object.ReferenceEquals(x, y) || (object)x == null && (object)y == null || x.Equals(y);
        public static bool operator !=(KzKeyPath x, KzKeyPath y) => !(x == y);

        /// <summary>
        /// Returns true if HardenedBit is set on last index.
        /// Throws InvalidOperation if there are no indices.
        /// </summary>
		public bool IsHardened
		{
			get {
                if (Count == 0) throw new InvalidOperationException("No index found in this KzHDKeyPath");
                return (Indices[Count - 1] & HardenedBit) != 0;
            }
        }

        public static implicit operator KzKeyPath(string s) => new KzKeyPath(s);
    }
}
