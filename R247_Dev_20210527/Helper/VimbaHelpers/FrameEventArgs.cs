using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Helper
{
    public class FrameEventArgs : EventArgs
    {
        /// <summary>
        /// The Image (data)
        /// </summary>
        private HImage m_Image = null;

        /// <summary>
        /// The Exception (data)
        /// </summary>
        private Exception m_Exception = null;

        /// <summary>
        /// Initializes a new instance of the FrameEventArgs class. 
        /// </summary>
        /// <param name="image">The Image to transfer</param>
        public FrameEventArgs(HImage image)
        {
            if (null == image)
            {
                throw new ArgumentNullException("image");
            }

            m_Image = image;
        }

        /// <summary>
        /// Initializes a new instance of the FrameEventArgs class. 
        /// </summary>
        /// <param name="exception">The Exception to transfer</param>
        public FrameEventArgs(Exception exception)
        {
            if (null == exception)
            {
                throw new ArgumentNullException("exception");
            }

            m_Exception = exception;
        }

        /// <summary>
        /// Gets the image 
        /// </summary>
        public HImage Image
        {
            get
            {
                return m_Image;
            }
        }

        /// <summary>
        /// Gets the Exception
        /// </summary>
        public Exception Exception
        {
            get
            {
                return m_Exception;
            }
        }
    }
}
