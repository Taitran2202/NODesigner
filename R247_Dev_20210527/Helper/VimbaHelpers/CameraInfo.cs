using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Helper
{
    public class CameraInfo
    {
        /// <summary>
        /// The camera name 
        /// </summary>
        private string m_Name = null;

        /// <summary>
        /// The camera ID
        /// </summary>
        private string m_ID = null;

        /// <summary>
        /// Initializes a new instance of the CameraInfo class.
        /// </summary>
        /// <param name="name">The camera name</param>
        /// <param name="id">The camera ID</param>
        public CameraInfo(string name, string id)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if (null == name)
            {
                throw new ArgumentNullException("id");
            }

            this.m_Name = name;
            this.m_ID = id;
        }

        /// <summary>
        /// Gets the name of the camera
        /// </summary>
        public string Name
        {
            get
            {
                return this.m_Name;
            }
        }

        /// <summary>
        /// Gets the ID
        /// </summary>
        public string ID
        {
            get
            {
                return this.m_ID;
            }
        }

        /// <summary>
        /// Overrides the toString Method for the CameraInfo class (this)
        /// </summary>
        /// <returns>The Name of the camera</returns>
        public override string ToString()
        {
            return this.m_Name;
        }
    }
}
