using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nito.Combinatorics
{
    /// <summary>
    /// Combinations defines a sequence of all possible subsets of a particular size from the set of values.
    /// Within the returned set, there is no prescribed order.
    /// This follows the mathematical concept of choose.
    /// For example, put <c>10</c> dominoes in a hat and pick <c>5</c>.
    /// The number of possible combinations is defined as "10 choose 5", which is calculated as <c>(10!) / ((10 - 5)! * 5!)</c>.
    /// </summary>
    /// <remarks>
    /// The MetaCollectionType parameter of the constructor allows for the creation of
    /// two types of sets,  those with and without repetition in the output set when 
    /// presented with repetition in the input set.
    /// 
    /// When given a input collect {A B C} and lower index of 2, the following sets are generated:
    /// MetaCollectionType.WithRepetition =>
    /// {A A}, {A B}, {A C}, {B B}, {B C}, {C C}
    /// MetaCollectionType.WithoutRepetition =>
    /// {A B}, {A C}, {B C}
    /// 
    /// Input sets with multiple equal values will generate redundant combinations in proportion
    /// to the likelihood of outcome.  For example, {A A B B} and a lower index of 3 will generate:
    /// {A A B} {A A B} {A B B} {A B B}
    /// </remarks>
    /// <typeparam name="T">The type of the values within the list.</typeparam>
    public sealed class Combinations<T> : IMetaCollection<T>
    {
        /// <summary>
        /// Create a combination set from the provided list of values.
        /// The upper index is calculated as values.Count, the lower index is specified.
        /// Collection type defaults to MetaCollectionType.WithoutRepetition
        /// </summary>
        /// <param name="values">List of values to select combinations from.</param>
        /// <param name="lowerIndex">The size of each combination set to return.</param>
        public Combinations(IList<T> values, int lowerIndex)
            : this(values, lowerIndex, GenerateOption.WithoutRepetition)
        {
        }

        /// <summary>
        /// Create a combination set from the provided list of values.
        /// The upper index is calculated as values.Count, the lower index is specified.
        /// </summary>
        /// <param name="values">List of values to select combinations from.</param>
        /// <param name="lowerIndex">The size of each combination set to return.</param>
        /// <param name="type">The type of Combinations set to generate.</param>
        public Combinations(IList<T> values, int lowerIndex, GenerateOption type)
        {
            _ = values ?? throw new ArgumentNullException(nameof(values));
            Initialize(values, lowerIndex, type);
        }

        /// <summary>
        /// Gets an enumerator for collecting the list of combinations.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<IList<T>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Gets an enumerator for collecting the list of combinations.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// The enumerator that enumerates each meta-collection of the enclosing Combinations class.
        /// </summary>
        public sealed class Enumerator : IEnumerator<IList<T>>
        {
            /// <summary>
            /// Construct a enumerator with the parent object.
            /// </summary>
            /// <param name="source">The source combinations object.</param>
            public Enumerator(Combinations<T> source)
            {
                _myParent = source;
                _myPermutationsEnumerator = (Permutations<bool>.Enumerator)_myParent._myPermutations.GetEnumerator();
            }

            /// <summary>
            /// Resets the combinations enumerator to the first combination.  
            /// </summary>
            public void Reset()
            {
                _myPermutationsEnumerator.Reset();
            }

            /// <summary>
            /// Advances to the next combination of items from the set.
            /// </summary>
            /// <returns>True if successfully moved to next combination, False if no more unique combinations exist.</returns>
            /// <remarks>
            /// The heavy lifting is done by the permutations object, the combination is generated
            /// by creating a new list of those items that have a true in the permutation parrellel array.
            /// </remarks>
            public bool MoveNext()
            {
                var ret = _myPermutationsEnumerator.MoveNext();
                _myCurrentList = null;
                return ret;
            }

            /// <summary>
            /// The current combination
            /// </summary>
            public IList<T> Current
            {
                get
                {
                    ComputeCurrent();
                    return _myCurrentList;
                }
            }

            /// <summary>
            /// The current combination
            /// </summary>
            object IEnumerator.Current
            {
                get
                {
                    ComputeCurrent();
                    return _myCurrentList;
                }
            }

            /// <summary>
            /// Cleans up non-managed resources, of which there are none used here.
            /// </summary>
            public void Dispose() => _myPermutationsEnumerator.Dispose();

            /// <summary>
            /// The only complex function of this entire wrapper, ComputeCurrent() creates
            /// a list of original values from the bool permutation provided.  
            /// The exception for accessing current (InvalidOperationException) is generated
            /// by the call to .Current on the underlying enumeration.
            /// </summary>
            /// <remarks>
            /// To compute the current list of values, the underlying permutation object
            /// which moves with this enumerator, is scanned differently based on the type.
            /// The items have only two values, true and false, which have different meanings:
            /// 
            /// For type WithoutRepetition, the output is a straightforward subset of the input array.  
            /// E.g. 6 choose 3 without repetition
            /// Input array:   {A B C D E F}
            /// Permutations:  {0 1 0 0 1 1}
            /// Generates set: {A   C D    }
            /// Note: size of permutation is equal to upper index.
            /// 
            /// For type WithRepetition, the output is defined by runs of characters and when to 
            /// move to the next element.
            /// E.g. 6 choose 5 with repetition
            /// Input array:   {A B C D E F}
            /// Permutations:  {0 1 0 0 1 1 0 0 1 1}
            /// Generates set: {A   B B     D D    }
            /// Note: size of permutation is equal to upper index - 1 + lower index.
            /// </remarks>
            private void ComputeCurrent()
            {
                if (_myCurrentList != null)
                {
                    return;
                }

                _myCurrentList = new List<T>();
                var index = 0;
                var currentPermutation = _myPermutationsEnumerator.Current;
                foreach (var p in currentPermutation)
                {
                    if (!p)
                    {
                        _myCurrentList.Add(_myParent._myValues[index]);
                        if (_myParent.Type == GenerateOption.WithoutRepetition)
                        {
                            ++index;
                        }
                    }
                    else
                    {
                        ++index;
                    }
                }
            }

            /// <summary>
            /// Parent object this is an enumerator for.
            /// </summary>
            private readonly Combinations<T> _myParent;

            /// <summary>
            /// The current list of values, this is lazy evaluated by the Current property.
            /// </summary>
            private List<T> _myCurrentList;

            /// <summary>
            /// An enumerator of the parents list of lexicographic orderings.
            /// </summary>
            private Permutations<bool>.Enumerator _myPermutationsEnumerator;
        }

        /// <summary>
        /// The number of unique combinations that are defined in this meta-collection.
        /// This value is mathematically defined as Choose(M, N) where M is the set size
        /// and N is the subset size.  This is M! / (N! * (M-N)!).
        /// </summary>
        public long Count => _myPermutations.Count;

        /// <summary>
        /// The type of Combinations set that is generated.
        /// </summary>
        public GenerateOption Type => _myMetaCollectionType;

        /// <summary>
        /// The upper index of the meta-collection, equal to the number of items in the initial set.
        /// </summary>
        public int UpperIndex => _myValues.Count;

        /// <summary>
        /// The lower index of the meta-collection, equal to the number of items returned each iteration.
        /// </summary>
        public int LowerIndex
        {
            get
            {
                return _myLowerIndex;
            }
        }

        /// <summary>
        /// Initialize the combinations by settings a copy of the values from the 
        /// </summary>
        /// <param name="values">List of values to select combinations from.</param>
        /// <param name="lowerIndex">The size of each combination set to return.</param>
        /// <param name="type">The type of Combinations set to generate.</param>
        /// <remarks>
        /// Copies the array and parameters and then creates a map of booleans that will 
        /// be used by a permutations object to reference the subset.  This map is slightly
        /// different based on whether the type is with or without repetition.
        /// 
        /// When the type is WithoutRepetition, then a map of upper index elements is
        /// created with lower index false's.  
        /// E.g. 8 choose 3 generates:
        /// Map: {1 1 1 1 1 0 0 0}
        /// Note: For sorting reasons, false denotes inclusion in output.
        /// 
        /// When the type is WithRepetition, then a map of upper index - 1 + lower index
        /// elements is created with the falses indicating that the 'current' element should
        /// be included and the trues meaning to advance the 'current' element by one.
        /// E.g. 8 choose 3 generates:
        /// Map: {1 1 1 1 1 1 1 1 0 0 0} (7 trues, 3 falses).
        /// </remarks>
        private void Initialize(IList<T> values, int lowerIndex, GenerateOption type)
        {
            _myMetaCollectionType = type;
            _myLowerIndex = lowerIndex;
            _myValues = new List<T>();
            _myValues.AddRange(values);
            var myMap = new List<bool>();
            if (type == GenerateOption.WithoutRepetition)
            {
                myMap.AddRange(_myValues.Select((t, i) => i < _myValues.Count - _myLowerIndex));
            }
            else
            {
                for (var i = 0; i < values.Count - 1; ++i)
                {
                    myMap.Add(true);
                }
                for (var i = 0; i < _myLowerIndex; ++i)
                {
                    myMap.Add(false);
                }
            }
            _myPermutations = new Permutations<bool>(myMap);
        }

        /// <summary>
        /// Copy of values object is initialized with, required for enumerator reset.
        /// </summary>
        private List<T> _myValues;

        /// <summary>
        /// Permutations object that handles permutations on booleans for combination inclusion.
        /// </summary>
        private Permutations<bool> _myPermutations;

        /// <summary>
        /// The type of the combination collection.
        /// </summary>
        private GenerateOption _myMetaCollectionType;

        /// <summary>
        /// The lower index defined in the constructor.
        /// </summary>
        private int _myLowerIndex;
    }
}