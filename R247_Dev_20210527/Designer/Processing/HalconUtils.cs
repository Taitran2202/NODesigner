using HalconDotNet;
using NumSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Processing
{
    public class HalconUtils
    {
        public static HImage PadImage(HImage imgInput, int smallest_div,out int originalw,out int originalh)
        {
            //int originalw, originalh;
            imgInput.GetImageSize(out originalw, out originalh);
            //make sure width and height divideable to 16

            int pad_h = 0;
            int pad_w = 0;
            HImage image_padded;
            if ((originalh % smallest_div != 0) | (originalw % smallest_div != 0))
            {
                pad_h = smallest_div - (originalh % smallest_div);
                pad_w = smallest_div - (originalw % smallest_div);
                image_padded = imgInput.ChangeFormat(originalw + pad_w, originalh + pad_h);
            }

            else
            {
                image_padded = imgInput;
            }

            return image_padded;
        }
        public static NDArray HImageToNDArray(HImage image_padded,int num_channel,out int input_width, out int input_height)
        {
            image_padded.GetImageSize(out input_width, out input_height);
            byte[] array_input = new byte[input_width * input_height * num_channel];
            string type;
            int width, height;
            if (num_channel == 3)
            {
                IntPtr pointerR, pointerG, pointerB;
                image_padded.GetImagePointer3(out pointerR, out pointerG, out pointerB, out type, out width, out height);
                Marshal.Copy(pointerR, array_input, 0, width * height);
                Marshal.Copy(pointerG, array_input, width * height, width * height);
                Marshal.Copy(pointerB, array_input, width * height * 2, width * height);
            }
            else
            {
                IntPtr pointerGray = image_padded.GetImagePointer1(out type, out width, out height);
                Marshal.Copy(pointerGray, array_input, 0, width * height);
            }

            
            return new NDArray(array_input, new NumSharp.Shape(1, num_channel, input_height, input_width), order: 'F');
        }
        public static byte[] HImageToByteArray(HImage image_padded, int num_channel, out int input_width, out int input_height)
        {
            image_padded.GetImageSize(out input_width, out input_height);
            byte[] array_input = new byte[input_width * input_height * num_channel];
            string type;
            int width, height;
            if (num_channel == 3)
            {
                IntPtr pointerR, pointerG, pointerB;
                image_padded.GetImagePointer3(out pointerR, out pointerG, out pointerB, out type, out width, out height);
                Marshal.Copy(pointerR, array_input, 0, width * height);
                Marshal.Copy(pointerG, array_input, width * height, width * height);
                Marshal.Copy(pointerB, array_input, width * height * 2, width * height);
            }
            else
            {
                IntPtr pointerGray = image_padded.GetImagePointer1(out type, out width, out height);
                Marshal.Copy(pointerGray, array_input, 0, width * height);
            }


            return array_input;
        }
        public static byte[] HImageJpeg(HImage image_padded, int num_channel, out int input_width, out int input_height)
        {
            image_padded.GetImageSize(out input_width, out input_height);
            byte[] array_input = new byte[input_width * input_height * num_channel];
            string type;
            int width, height;
            OpenCvSharp.Mat rgb = new OpenCvSharp.Mat();
            if (num_channel == 3)
            {
                IntPtr pointerR, pointerG, pointerB;
                image_padded.GetImagePointer3(out pointerR, out pointerG, out pointerB, out type, out width, out height);
                OpenCvSharp.Mat rMat = new OpenCvSharp.Mat(height, width, OpenCvSharp.MatType.CV_8U, pointerR);
                OpenCvSharp.Mat gMat = new OpenCvSharp.Mat(height, width, OpenCvSharp.MatType.CV_8U, pointerG);
                OpenCvSharp.Mat bMat = new OpenCvSharp.Mat(height, width, OpenCvSharp.MatType.CV_8U, pointerB);
                
                OpenCvSharp.Cv2.Merge(new OpenCvSharp.Mat[] { bMat, gMat, rMat }, rgb);

            }
            else
            {
                IntPtr pointerGray = image_padded.GetImagePointer1(out type, out width, out height);
                rgb = new OpenCvSharp.Mat(height, width, OpenCvSharp.MatType.CV_8SC1, pointerGray);
            }
            return rgb.ImEncode(".jpg");
        }
        public static float[] HImageToFloatArray(HImage image_padded, int num_channel, out int input_width, out int input_height,bool swapChannel =false)
        {
            image_padded.GetImageSize(out input_width, out input_height);
            float[] array_input = new float[input_width * input_height * num_channel];
            string type;
            int width, height;
            if (num_channel == 3)
            {
                IntPtr pointerR, pointerG, pointerB;
                image_padded.GetImagePointer3(out pointerR, out pointerG, out pointerB, out type, out width, out height);
                if (swapChannel)
                {
                    Marshal.Copy(pointerB, array_input, 0, width * height);
                    Marshal.Copy(pointerG, array_input, width * height, width * height);
                    Marshal.Copy(pointerR, array_input, width * height * 2, width * height);
                }
                else
                {
                    Marshal.Copy(pointerR, array_input, 0, width * height);
                    Marshal.Copy(pointerG, array_input, width * height, width * height);
                    Marshal.Copy(pointerB, array_input, width * height * 2, width * height);
                }
                
            }
            else
            {
                IntPtr pointerGray = image_padded.GetImagePointer1(out type, out width, out height);
                Marshal.Copy(pointerGray, array_input, 0, width * height);
            }


            return array_input;
        }
    }
}
