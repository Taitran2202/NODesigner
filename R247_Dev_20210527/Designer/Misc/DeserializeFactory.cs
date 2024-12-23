using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Misc
{
    public class DeserializeFactory
    {
        public HFile file;
        public HSerializedItem serialize_item;
        public string path;
        public DeserializeFactory(HFile file, HSerializedItem serialize_item, string path)
        {

            this.file = file;
            this.serialize_item = serialize_item;
            this.path = path;
        }
        public HTuple DeserializeTuple()
        {
            serialize_item.FreadSerializedItem(file);
            return HTuple.DeserializeTuple(serialize_item);
        }
        public void DeserializeClassTrainData(HClassTrainData train_data)
        {
            serialize_item.FreadSerializedItem(file);
            train_data.DeserializeClassTrainData(serialize_item);
        }
        public HRegion DeserializeRegion()
        {
            serialize_item.FreadSerializedItem(file);
            HRegion region = new HRegion();
            region.DeserializeRegion(serialize_item);
            return region;
        }

        public HImage DeserializeImage()
        {
            serialize_item.FreadSerializedItem(file);
            HImage image = new HImage();
            image.DeserializeImage(serialize_item);
            return image;
        }
        public HHomMat2D DeserializeHommat2d()
        {
            serialize_item.FreadSerializedItem(file);
            HHomMat2D value = new HHomMat2D();
            value.DeserializeHomMat2d(serialize_item);
            return value;
        }
        public HMetrologyModel DeserializeMetrologyModel()
        {
            serialize_item.FreadSerializedItem(file);
            HMetrologyModel value = new HMetrologyModel();
            value.DeserializeMetrologyModel(serialize_item);
            return value;
        }
        public HTextModel DeserializeTextModel()
        {
            serialize_item.FreadSerializedItem(file);
            HTextModel value = new HTextModel();
            //value.(serialize_item);
            return value;
        }
        public HNCCModel DeserializeNccModel()
        {
            serialize_item.FreadSerializedItem(file);
            HNCCModel value = new HNCCModel();
            value.DeserializeNccModel(serialize_item);
            return value;
        }
        public HBarCode DeserializeBarcode()
        {
            serialize_item.FreadSerializedItem(file);
            HBarCode value = new HBarCode();
            value.DeserializeBarCodeModel(serialize_item);
            return value;
        }
        public HMeasure DeserializeMeasure()
        {
            serialize_item.FreadSerializedItem(file);
            HMeasure value = new HMeasure();
            value.DeserializeMeasure(serialize_item);
            return value;
        }
        public HClassMlp DeserializeClassMlp()
        {
            serialize_item.FreadSerializedItem(file);
            HClassMlp value = new HClassMlp();
            value.DeserializeClassMlp(serialize_item);
            return value;
        }
        public HVariationModel DeserializeVariationModel()
        {
            serialize_item.FreadSerializedItem(file);
            HVariationModel value = new HVariationModel();
            value.DeserializeVariationModel(serialize_item);
            return value;
        }
        public HShapeModel DeserializeShapeModel()
        {
            serialize_item.FreadSerializedItem(file);
            HShapeModel value = new HShapeModel();
            value.DeserializeShapeModel(serialize_item);
            return value;
        }
        public HTextureInspectionModel DeserializeTextureModel()
        {
            serialize_item.FreadSerializedItem(file);
            HTextureInspectionModel value = new HTextureInspectionModel();
            value.DeserializeTextureInspectionModel(serialize_item);
            return value;
        }
    }
}
