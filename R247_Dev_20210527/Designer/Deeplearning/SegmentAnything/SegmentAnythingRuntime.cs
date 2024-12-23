using HalconDotNet;
using Microsoft.ML.OnnxRuntime;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Deeplearning.SegmentAnything
{
    public class SegmentAnythingRuntime
    {
        private static SegmentAnythingRuntime _instance;
        public static SegmentAnythingRuntime Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new SegmentAnythingRuntime();
                }
                return _instance;
            }
        }
        public string EncoderDir { get; set; }
        public string DecoderDir { get; set; }
        public InferenceSession EncoderSession { get; set; }
        public InferenceSession DecoderSession { get; set; }

        public ModelState EncoderState = ModelState.Unloaded;
        public ModelState DecoderState = ModelState.Unloaded;
        public int OriWidth { get; set; }
        public int OriHeight { get; set; }
        public int ImageSize { get; set; }
        public bool LoadEncoder()
        {
            try
            {
                if (!System.IO.File.Exists(EncoderDir))
                {
                    EncoderState = ModelState.NotFound;
                    return false;
                }
                EncoderSession = new InferenceSession(EncoderDir);



                EncoderState = ModelState.Loaded;

            }
            catch (Exception ex)
            {
                EncoderState = ModelState.Error;
                return false;
            }

            return true;
        }
        public bool LoadDecoder()
        {
            try
            {
                if (!System.IO.File.Exists(DecoderDir))
                {
                    DecoderState = ModelState.NotFound;
                    return false;
                }
                DecoderSession = new InferenceSession(DecoderDir);



                DecoderState = ModelState.Loaded;

            }
            catch (Exception ex)
            {
                DecoderState = ModelState.Error;
                return false;
            }

            return true;
        }
        private SegmentAnythingRuntime()
        {
            ImageSize = 1024;
            DecoderDir = "C:\\SegmentAnything\\decoder-b-quant.onnx";
            EncoderDir = "C:\\SegmentAnything\\encoder-b-quant.onnx";
            LoadDecoder();
            LoadEncoder();
        }
        public Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float> CurrentEmbedding;
        public Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float> CurrentMaskInput;
        public Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float> CurrentMaskIndicator;
        public void Reset()
        {
            CurrentMaskInput = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new int[] { 1, 1, 256, 256 });
            CurrentMaskIndicator = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new int[] { 1 });
            Points.Clear();
        }
        public void SetImage(HImage image)
        {
            image.GetImageSize(out int w, out int h);
            OriHeight = h;
            OriWidth = w;
            var imageResized=image.ZoomImageSize(ImageSize, ImageSize, "bilinear");
            var imageNorm = Normalize(imageResized);
            var array_final = Processing.HalconUtils.HImageToFloatArray(imageNorm, 3, out int _, out int _);
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor("x",new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(array_final,new int[]{1,3,ImageSize,ImageSize }))
            };
            CurrentEmbedding = EncoderSession.Run(inputs).ToList()[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            CurrentMaskInput = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new int[] { 1, 1, 256, 256 });
            CurrentMaskIndicator = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new int[] { 1});
            Points.Clear();
        }
        public List<InputPoint> Points = new List<InputPoint>();
        public HRegion AddPoint(float x, float y)
        {
            Console.WriteLine("{0},{1}", x, y);
            Points.Add(new InputPoint(x, y,1));
            int total = Points.Count;
            float[] pointsArray =new float[] { 0, 0 }.Concat(Points.SelectMany(item => new[] { item.X*1024/OriWidth, item.Y*1024/OriHeight }).ToArray()).ToArray();
            float[] labelArray = new float[] { -1 }.Concat(Points.SelectMany(item => new[] { item.Class}).ToArray()).ToArray();
            CurrentMaskIndicator = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[] { 1f }, new int[] { 1 });
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor("image_embeddings",CurrentEmbedding),
                NamedOnnxValue.CreateFromTensor("point_coords",new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(pointsArray,new int[]{1, total+1, 2})),
                NamedOnnxValue.CreateFromTensor("point_labels",new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(labelArray,new int[]{1,total+1})),
                NamedOnnxValue.CreateFromTensor("mask_input",CurrentMaskInput),
                NamedOnnxValue.CreateFromTensor("has_mask_input",CurrentMaskIndicator),
                NamedOnnxValue.CreateFromTensor("orig_im_size",new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{1024,1024},new int[]{2}))
            };
            var result = DecoderSession.Run(inputs).ToList();
            var mask = result[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            CurrentMaskInput = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>((result[2].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).Buffer.Slice(0, 256 * 256).ToArray() ,new int[] { 1, 1, 256, 256 }); 
            var resultMask = new HImage("real", 1024, 1024);
            IntPtr pointerResult = resultMask.GetImagePointer1(out HTuple type, out HTuple width, out HTuple height);

            Marshal.Copy(mask.ToArray(), 0, pointerResult, width * height);
            return (resultMask).Threshold(0.0, 255).ZoomRegion(OriWidth / 1024.0, OriHeight / 1024.0);
        }
        public HRegion RemovePoint(float x, float y)
        {
            Points.Add(new InputPoint(x, y, 0));
            int total = Points.Count;
            float[] pointsArray = new float[] { 0, 0 }.Concat(Points.SelectMany(item => new[] { item.X * 1024 / OriWidth, item.Y * 1024 / OriHeight }).ToArray()).ToArray();
            float[] labelArray = new float[] { -1 }.Concat(Points.SelectMany(item => new[] { item.Class }).ToArray()).ToArray();
            CurrentMaskIndicator = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[] { 1f }, new int[] { 1 });
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor("image_embeddings",CurrentEmbedding),
                NamedOnnxValue.CreateFromTensor("point_coords",new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(pointsArray,new int[]{1, total+1, 2})),
                NamedOnnxValue.CreateFromTensor("point_labels",new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(labelArray,new int[]{1,total+1})),
                NamedOnnxValue.CreateFromTensor("mask_input",CurrentMaskInput),
                NamedOnnxValue.CreateFromTensor("has_mask_input",CurrentMaskIndicator),
                NamedOnnxValue.CreateFromTensor("orig_im_size",new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{1024,1024},new int[]{2}))
            };
            var result = DecoderSession.Run(inputs).ToList();
            var mask = result[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            CurrentMaskInput = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>((result[2].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).Buffer.Slice(0, 256 * 256).ToArray(), new int[] { 1, 1, 256, 256 });
            var resultMask = new HImage("real", 1024, 1024);
            IntPtr pointerResult = resultMask.GetImagePointer1(out HTuple type, out HTuple width, out HTuple height);

            Marshal.Copy(mask.ToArray(), 0, pointerResult, width * height);
            return (resultMask).Threshold(0.0, 255).ZoomRegion(OriWidth / 1024.0, OriHeight / 1024.0);
        }
        public HRegion RunDecoder(float x,float y)
        {
            Console.WriteLine("{0},{1}", x, y);
            CurrentMaskIndicator = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[] { 1f }, new int[] { 1 });
            var inputs = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor("image_embeddings",CurrentEmbedding),
                NamedOnnxValue.CreateFromTensor("point_coords",new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{0f,0f,x*1024/OriWidth,y*1024/OriHeight},new int[]{1,2,2})),
                NamedOnnxValue.CreateFromTensor("point_labels",new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{-1,1},new int[]{1,2})),
                NamedOnnxValue.CreateFromTensor("mask_input",CurrentMaskInput),
                NamedOnnxValue.CreateFromTensor("has_mask_input",CurrentMaskIndicator),
                NamedOnnxValue.CreateFromTensor("orig_im_size",new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(new float[]{1024,1024},new int[]{2}))
            };
            var result = DecoderSession.Run(inputs).ToList();
            var mask = result[0].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>;
            //Console.WriteLine(mask.Dimensions[1]);
            //CurrentMaskInput = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>((result[2].Value as Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>).Buffer.Slice(0, 256 * 256).ToArray(), new int[] { 1, 1, 256, 256 });
            var resultMask = new HImage("real", 1024, 1024);
            IntPtr pointerResult = resultMask.GetImagePointer1(out HTuple type, out HTuple width, out HTuple height);

            Marshal.Copy(mask.ToArray(),0, pointerResult, width * height);
            return (resultMask).Threshold(0.0, 255).ZoomRegion(OriWidth / 1024.0, OriHeight / 1024.0) ;
        }
        public HImage Normalize(HImage imageInput)
        {
            HImage image = imageInput.ConvertImageType("float") / 255.0;
            HImage image1 = image.Decompose3(out HImage image2, out HImage image3);
            image1 = (image1 - 0.485) / 0.229;
            image2 = (image2 - 0.456) / 0.224;
            image3 = (image3 - 0.406) / 0.225;
            return image1.Compose3(image2, image3);
        }
    }
    public class InputPoint
    {
        public float Class { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public InputPoint(float x,float y, float Class)
        {
            this.X = x;
            this.Y = y;
            this.Class = Class;
        }
    }
}
