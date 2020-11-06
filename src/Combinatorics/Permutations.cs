using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Combinatorics.Collections
{
    /// <summary>
    /// Permutations defines a sequence of all possible orderings of a set of values.
    /// </summary>
    /// <remarks>
    /// When given a input collect {A A B}, the following sets are generated:
    /// MetaCollectionType.WithRepetition =>
    /// {A A B}, {A B A}, {A A B}, {A B A}, {B A A}, {B A A}
    /// MetaCollectionType.WithoutRepetition =>
    /// {A A B}, {A B A}, {B A A}
    /// 
    /// When generating non-repetition sets, ordering is based on the lexicographic 
    /// ordering of the lists based on the provided Comparer.  
    /// If no comparer is provided, then T must be IComparable on T.
    /// 
    /// When generating repetition sets, no comparisons are performed and therefore
    /// no comparer is required and T does not need to be IComparable.
    /// </remarks>
    /// <typeparam name="T">The type of the values within the list.</typeparam>
    public sealed class Permutations<T> : IEnumerable<IReadOnlyList<T>>
    {
        /// <summary>
        /// Create a permutation set from the provided list of values.  
        /// The values (T) must implement IComparable.  
        /// If T does not implement IComparable use a constructor with an explicit IComparer.
        /// The repetition type defaults to MetaCollectionType.WithholdRepetitionSets
        /// </summary>
        /// <param name="values">List of values to permute.</param>
        public Permutations(IEnumerable<T> values)
            : this(values, GenerateOptions.None, null)
        {
        }

        /// <summary>
        /// Create a permutation set from the provided list of values.  
        /// If type is MetaCollectionType.WithholdRepetitionSets, then values (T) must implement IComparable.  
        /// If T does not implement IComparable use a constructor with an explicit IComparer.
        /// </summary>
        /// <param name="values">List of values to permute.</param>
        /// <param name="type">The type of permutation set to calculate.</param>
        public Permutations(IEnumerable<T> values, GenerateOption type)
            : this(values, type, null)
        {
        }

        /// <summary>
        /// Create a permutation set from the provided list of values.  
        /// The values will be compared using the supplied IComparer.
        /// The repetition type defaults to MetaCollectionType.WithholdRepetitionSets
        /// </summary>
        /// <param name="values">List of values to permute.</param>
        /// <param name="comparer">Comparer used for defining the lexicographic order.</param>
        public Permutations(IEnumerable<T> values, IComparer<T>? comparer)
            : this(values, GenerateOptions.None, comparer)
        {
        }

        /// <summary>
        /// Create a permutation set from the provided list of values.  
        /// If type is MetaCollectionType.WithholdRepetitionSets, then the values will be compared using the supplied IComparer.
        /// </summary>
        /// <param name="values">List of values to permute.</param>
        /// <param name="type">The type of permutation set to calculate.</param>
        /// <param name="comparer">Comparer used for defining the lexicographic order.</param>
        public Permutations(IEnumerable<T> values, GenerateOption type, IComparer<T>? comparer)
            : this(values, type == GenerateOption.WithRepetition ? GenerateOptions.WithRepetition : GenerateOptions.None, comparer)
        {

        }

        /// <summary>
        /// Create a permutation set from the provided list of values.  
        /// If type is MetaCollectionType.WithholdRepetitionSets, then the values will be compared using the supplied IComparer.
        /// </summary>
        /// <param name="values">List of values to permute.</param>
        /// <param name="flags">The type of permutation set to calculate.</param>
        /// <param name="comparer">Comparer used for defining the lexicographic order.</param>
        public Permutations(IEnumerable<T> values, GenerateOptions flags, IComparer<T>? comparer)
        {
            _ = values ?? throw new ArgumentNullException(nameof(values));

            // Copy information provided and then create a parallel int array of lexicographic
            // orders that will be used for the actual permutation algorithm.  
            // The input array is first sorted as required for WithoutRepetition and always just for consistency.
            // This array is constructed one of two way depending on the type of the collection.
            //
            // When type is MetaCollectionType.WithRepetition, then all N! permutations are returned
            // and the lexicographic orders are simply generated as 1, 2, ... N.  
            // E.g.
            // Input array:          {A A B C D E E}
            // Lexicographic Orders: {1 2 3 4 5 6 7}
            // 
            // When type is MetaCollectionType.WithoutRepetition, then fewer are generated, with each
            // identical element in the input array not repeated.  The lexicographic sort algorithm
            // handles this natively as long as the repetition is repeated.
            // E.g.
            // Input array:          {A A B C D E E}
            // Lexicographic Orders: {1 1 2 3 4 5 5}

            Flags = flags;
            _myValues = values.ToList();
            _myLexicographicOrders = new int[_myValues.Count];

            if (Type == GenerateOption.WithRepetition)
            {
                for (var i = 0; i < _myLexicographicOrders.Length; ++i)
                {
                    _myLexicographicOrders[i] = i;
                }
            }
            else
            {
                comparer ??= Comparer<T>.Default;

                _myValues.Sort(comparer);
                var j = 1;
                if (_myLexicographicOrders.Length > 0)
                {
                    _myLexicographicOrders[0] = j;
                }

                for (var i = 1; i < _myLexicographicOrders.Length; ++i)
                {
                    if (comparer.Compare(_myValues[i - 1], _myValues[i]) != 0)
                    {
                        ++j;
                    }

                    _myLexicographicOrders[i] = j;
                }
            }

            _count = new Lazy<BigInteger>(GetCount);
        }

        /// <summary>
        /// Gets an enumerator for collecting the list of permutations.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<IReadOnlyList<T>> GetEnumerator()
        {
            var lexicographicalOrders = _myLexicographicOrders.ToArray();
            var values = new List<T>(_myValues);
            yield return AllowMutation ? _myValues : new List<T>(_myValues);
            if (values.Count < 2)
                yield break;

            while (true)
            {
                var i = lexicographicalOrders.Length - 1;

                while (lexicographicalOrders[i - 1] >= lexicographicalOrders[i])
                {
                    --i;
                    if (i == 0)
                        yield break;
                }

                var j = lexicographicalOrders.Length;

                while (lexicographicalOrders[j - 1] <= lexicographicalOrders[i - 1])
                    --j;

                Swap(i - 1, j - 1);

                ++i;

                j = lexicographicalOrders.Length;
                while (i < j)
                {
                    Swap(i - 1, j - 1);
                    ++i;
                    --j;
                }

                yield return AllowMutation ? _myValues : new List<T>(_myValues);
            }

            void Swap(int i, int j)
            {
                var valueTemp = values[i];
                values[i] = values[j];
                values[j] = valueTemp;
                var orderTemp = lexicographicalOrders[i];
                lexicographicalOrders[i] = lexicographicalOrders[j];
                lexicographicalOrders[j] = orderTemp;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// The count of all permutations that will be returned.
        /// If <see cref="Type"/> is <see cref="GenerateOption.WithoutRepetition"/>, then this does not count equivalent result sets.  
        /// I.e., count of permutations of "AAB" will be 3 instead of 6.  
        /// If <see cref="Type"/> is <see cref="GenerateOption.WithRepetition"/>, then this is all combinations and is therefore N!, where N is the number of values in the input set.
        /// </summary>
        public BigInteger Count => _count.Value;

        /// <summary>
        /// The flags used to generate the result sets.
        /// </summary>
        public GenerateOptions Flags { get; }

        /// <summary>
        /// The type of permutations set that is generated.
        /// </summary>
        public GenerateOption Type => (Flags & GenerateOptions.WithRepetition) != 0 ? GenerateOption.WithRepetition : GenerateOption.WithoutRepetition;

        /// <summary>
        /// The upper index of the meta-collection, equal to the number of items in the input set.
        /// </summary>
        public int UpperIndex => _myValues.Count;

        /// <summary>
        /// The lower index of the meta-collection, equal to the number of items returned each iteration.
        /// This is always equal to <see cref="UpperIndex"/>.
        /// </summary>
        public int LowerIndex => _myValues.Count;

        private bool AllowMutation => (Flags & GenerateOptions.AllowMutation) != 0;

        /// <summary>
        /// Calculates the total number of permutations that will be returned.  
        /// As this can grow very large, extra effort is taken to avoid overflowing the accumulator.  
        /// While the algorithm looks complex, it really is just collecting numerator and denominator terms
        /// and cancelling out all of the denominator terms before taking the product of the numerator terms.  
        /// </summary>
        /// <returns>The number of permutations.</returns>
        private BigInteger GetCount()
        {
            var runCount = 1;
            var divisors = Enumerable.Empty<int>();
            var numerators = Enumerable.Empty<int>();

            for (var i = 1; i < _myLexicographicOrders.Length; ++i)
            {
                numerators = numerators.Concat(SmallPrimeUtility.Factor(i + 1));

                if (_myLexicographicOrders[i] == _myLexicographicOrders[i - 1])
                {
                    ++runCount;
                }
                else
                {
                    for (var f = 2; f <= runCount; ++f)
                        divisors = divisors.Concat(SmallPrimeUtility.Factor(f));

                    runCount = 1;
                }
            }

            for (var f = 2; f <= runCount; ++f)
                divisors = divisors.Concat(SmallPrimeUtility.Factor(f));

            return SmallPrimeUtility.EvaluatePrimeFactors(
                SmallPrimeUtility.DividePrimeFactors(numerators, divisors)
            );
        }

        /// <summary>
        /// A list of T that represents the order of elements as originally provided.
        /// </summary>
        private readonly List<T> _myValues;

        /// <summary>
        /// Parallel array of integers that represent the location of items in the myValues array.
        /// This is generated at Initialization and is used as a performance speed up rather that
        /// comparing T each time, much faster to let the CLR optimize around integers.
        /// </summary>
        private readonly int[] _myLexicographicOrders;

        /// <summary>
        /// Lazy-calculated <see cref="Count"/>.
        /// </summary>
        private readonly Lazy<BigInteger> _count;
    }
}
