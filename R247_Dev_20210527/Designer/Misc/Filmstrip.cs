using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ReactiveUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NOVisionDesigner.Designer.Misc
{
    [JsonObject]
    public class ImageMetaData:ReactiveUI.ReactiveObject
    {
        string Dir;
        public Rect1 ROI { get; set; }
        DatasetSplit _dataset = DatasetSplit.train;
        [JsonConverter(typeof(StringEnumConverter))]
        public DatasetSplit Dataset
        {
            get => _dataset;
            set => this.RaiseAndSetIfChanged(ref _dataset, value);
        }
        public void Save()
        {
            System.IO.File.WriteAllText(Dir, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public ImageMetaData(string dir)
        {
            this.Dir = dir;
        }
        public ImageMetaData()
        {

        }
        public static ImageMetaData Create(string dir)
        {
            if (System.IO.File.Exists(dir))
            {
                var json = File.ReadAllText(dir);
                var config = JsonConvert.DeserializeObject<ImageMetaData>(json);
                config.Dir = dir;
                return config;
            }
            else
            {
                return new ImageMetaData(dir);
            }
        }
    }
    public enum DatasetSplit
    {
        train,test
    }
    public class ImageSet:ImageFilmstrip
    {
        public DateTime DateTime { get; set; }
        public ImageSet(string dir) : base(dir)
        {
            MetaDataDir = FullPath + ".meta";
            MetaData = ImageMetaData.Create(MetaDataDir);
        }
        public string MetaDataDir { get; set; }
        /// <summary>
        /// meta data file locate in same folder with image file and have extension .meta
        /// </summary>
        ImageMetaData _meta_data;
        public ImageMetaData MetaData
        {
            get
            {
                return _meta_data;
            }
            set
            {
                if (_meta_data != value)
                {
                    _meta_data = value;
                    RaisePropertyChanged("MetaData");
                }
            }
        }
        public void SaveMetaData()
        {
            MetaData.Save();
        }
    }
    public class ImageFilmstrip : INotifyPropertyChanged
    {

        public static object image_loader = new object();
        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public List<string> Tags { get; set; } = new List<string>();

        string _full_path;


        public string FullPath
        {
            get
            {
                return _full_path;
            }
            set
            {
                if (_full_path != value)
                {
                    _full_path = value;

                }
            }
        }
        string file_name;
        public string FileName
        {
            get
            {
                return file_name;
            }
            set
            {
                if (file_name != value)
                {
                    file_name = value;
                    RaisePropertyChanged("FileName");
                }
            }
        }

        bool _is_loaded = true;
        public bool IsLoaded
        {
            get
            {
                return _is_loaded;
            }
            set
            {
                if (_is_loaded != value)
                {
                    _is_loaded = value;
                    RaisePropertyChanged("IsLoaded");
                }
            }
        }
        public string Tag { get; set; }
        

        public double Score { get; set; }
        private System.Windows.Media.ImageSource _image;
        public System.Windows.Media.ImageSource Image
        {
            get
            {
                if (IsLoaded)
                {
                    Task.Run(new Action(() =>
                    {
                        lock (image_loader)
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.DecodePixelHeight = 140;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(_full_path);
                            bitmap.EndInit();
                            bitmap.Freeze();
                            //Thread.Sleep(500);
                            IsLoaded = false;
                            Image = bitmap;

                        }
                    }));
                    return null;
                }

                return _image;

            }
            internal set
            {
                _image = value;
                RaisePropertyChanged("Image");
            }

        }
        public ImageFilmstrip(string FullPath)
        {
            this.FullPath = FullPath;
            try
            {
                FileName = System.IO.Path.GetFileName(FullPath);
            }catch(Exception ex)
            {
                FileName = "Error File Name";
            }
            
        }
        public static BitmapImage Convert(System.Drawing.Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
    public interface IObjectGenerator<T>
    {
        /// <summary>
        /// Returns the number of items in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Generate the item that is located on the specified index.
        /// </summary>
        /// <remarks>
        /// This method is only be called once per index.
        /// </remarks>
        /// <param name="index">Index of the items that must be generated.</param>
        /// <returns>Fresh new instance.</returns>
        T CreateObject(int index);
    }
    public class VirtualList<T> : IList<T>, IList where T : class
    {
        #region Internal attributes
        /// <summary>
        /// Object that is used to generate the requested objects.
        /// </summary>
        /// <remarks>
        /// This object can also hold a IMultipleObjectGenerator reference.
        /// </remarks>
        private readonly IObjectGenerator<T> _generator;

        /// <summary>
        /// Internal array that holds the cached items.
        /// </summary>
        private readonly T[] _cachedItems;
        #endregion


        #region Constructor
        /// <summary>
        /// Create the virtual list.
        /// </summary>
        /// <param name="generator"></param>
        public VirtualList(IObjectGenerator<T> generator)
        {
            // Determine the number of items
            int maxItems = generator.Count;

            // Save generator and items
            _generator = generator;
            _cachedItems = new T[maxItems];
        }
        #endregion


        #region IList<T> Members
        public int IndexOf(T item)
        {
            return IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException("VirtualList is a read-only collection.");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("VirtualList is a read-only collection.");
        }

        public T this[int index]
        {
            get
            {
                // Cache item if it isn't cached already
                if (!IsItemCached(index))
                    CacheItem(index);

                // Return the cached object
                return _cachedItems[index];
            }
            set { throw new NotSupportedException("VirtualList is a read-only collection."); }
        }
        #endregion


        #region ICollection<T> Members
        public void Add(T item)
        {
            throw new NotSupportedException("VirtualList is a read-only collection.");
        }

        public void Clear()
        {
            throw new NotSupportedException("VirtualList is a read-only collection.");
        }

        public bool Contains(T item)
        {
            return (IndexOf(item) != -1);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _cachedItems.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _cachedItems.Length; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException("VirtualList is a read-only collection.");
        }
        #endregion


        #region IEnumerable<T> Members
        public IEnumerator<T> GetEnumerator()
        {
            return new VirtualEnumerator(this);
        }
        #endregion


        #region IList Members
        public int Add(object value)
        {
            throw new NotSupportedException("VirtualList is a read-only collection.");
        }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            int items = _cachedItems.Length;
            for (int index = 0; index < items; ++index)
            {
                // Check if item is found
                if (_cachedItems[index].Equals(value))
                    return index;
            }

            // Item not found
            return -1;
        }

        public void Insert(int index, object value)
        {
            throw new NotSupportedException("VirtualList is a read-only collection.");
        }

        public bool IsFixedSize
        {
            get { return true; }
        }

        public void Remove(object value)
        {
            throw new NotSupportedException("VirtualList is a read-only collection.");
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException("VirtualList is a read-only collection."); }
        }
        #endregion


        #region ICollection Members
        public void CopyTo(Array array, int index)
        {
            _cachedItems.CopyTo(array, index);
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }
        #endregion


        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new VirtualEnumerator(this);
        }
        #endregion


        #region Internal helper methods required for caching
        private bool IsItemCached(int index)
        {
            // If the object is NULL, then it is empty
            return (_cachedItems[index] != null);
        }
        #endregion

        public void CacheItem(int index)
        {
            // Obtain only a single object
            _cachedItems[index] = _generator.CreateObject(index);
        }


        #region Internal IEnumerator implementation
        private class VirtualEnumerator : IEnumerator<T>
        {
            private readonly VirtualList<T> _collection;
            private int _cursor;

            public VirtualEnumerator(VirtualList<T> collection)
            {
                _collection = collection;
                _cursor = 0;
            }

            public T Current
            {
                get { return _collection[_cursor]; }
            }


            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                // Check if we are behind
                if (_cursor == _collection.Count)
                    return false;

                // Increment cursor
                ++_cursor;
                return true;
            }

            public void Reset()
            {
                // Reset cursor
                _cursor = 0;
            }


            public void Dispose()
            {
                // NOP
            }

        }
        #endregion

    }
    public class ObjectGenerator : IObjectGenerator<ImageFilmstrip>
    {
        /// <summary>
        /// Internal attribute that holds to total number of items that
        /// can be generated.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="items">
        /// Total number of items that can be generated.
        /// </param>

        public ObjectGenerator(string path)
        {
            // _count = EmployeeService.NumberOfEmployees;
            string[] files = Directory.GetFiles(path, "*.bmp");
            this.files = files.Select(f => new ImageFilmstrip(f)).ToList<ImageFilmstrip>();
            this._count = files.Length;
        }
        List<ImageFilmstrip> files = new List<ImageFilmstrip>();
        #region IObjectGenerator<SampleObject> Members
        /// <summary>
        /// Number of items that are generated by this object.
        /// </summary>
        public int Count
        {
            get
            {
                // Return the number of objects in the array
                return _count;
            }
        }

        /// <summary>
        /// Create the object at the specified index.
        /// </summary>
        /// <param name="index">Object index.</param>
        /// <returns>New object instance.</returns>
        public ImageFilmstrip CreateObject(int index)
        {
            return files[index];
        }
        #endregion
    }
}
