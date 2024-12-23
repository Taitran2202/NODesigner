using HalconDotNet;
using NodeNetwork.ViewModels;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NOVisionDesigner.Designer.Misc
{
    public abstract class HelperMethods : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public static void LoadParam(DeserializeFactory item, object target)
        {
            PropertyInfo[] infos = target.GetType().GetProperties(System.Reflection.BindingFlags.Public
    | System.Reflection.BindingFlags.Instance
    | System.Reflection.BindingFlags.SetProperty);
            string[] member_list = new string[infos.Length];
            for (int i = 0; i < infos.Length; i++)
            {
                member_list[i] = infos[i].Name;
            }
            while (true)
            {

                HTuple value = item.DeserializeTuple();
                if (value == "EndSaveClass")
                {
                    break;
                }
                else
                {
                    string param_name = value[0];
                    int index = Array.IndexOf(member_list, param_name);
                    if (index > -1)
                    {
                        Type type = infos[index].PropertyType;


                        if (type.IsGenericType)
                        {
                            if (type.GetGenericTypeDefinition() == typeof(ObservableCollection<>))
                            {
                                if (typeof(IHalconDeserializable).IsAssignableFrom(type.GetGenericArguments()[0]))
                                {
                                    var typeinside = type.GetGenericArguments()[0];
                                    int count = value[1];
                                    var loaded_collection = (infos[index].GetValue(target));
                                    for (int i = 0; i < count; i++)
                                    {
                                        var instance=(IHalconDeserializable)Activator.CreateInstance(typeinside);
                                        instance.Load(item);
                                        loaded_collection.GetType().GetMethod("Add").Invoke(loaded_collection,new object[] { instance });
                                    }
                                    infos[index].SetValue(target, loaded_collection);
                                    continue;
                                }
                            }
                            if (type.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                if (typeof(IHalconDeserializable).IsAssignableFrom(type.GetGenericArguments()[0]))
                                {
                                    var typeinside = type.GetGenericArguments()[0];
                                    int count = value[1];
                                    var loaded_collection = (infos[index].GetValue(target));
                                    for (int i = 0; i < count; i++)
                                    {
                                        var instance = (IHalconDeserializable)Activator.CreateInstance(typeinside);
                                        instance.Load(item);
                                        loaded_collection.GetType().GetMethod("Add").Invoke(loaded_collection, new object[] { instance });
                                    }
                                    infos[index].SetValue(target, loaded_collection);
                                    continue;
                                }
                            }
                        }
                        if (typeof(IHalconDeserializable).IsAssignableFrom(type))
                        {
                            if (value[1] == 1)
                            {
                                var propvalue = infos[index].GetValue(target) as IHalconDeserializable;
                                if (propvalue == null)
                                {
                                    var propinstance = Activator.CreateInstance(type);
                                    infos[index].SetValue(target, propinstance);
                                }
                                (infos[index].GetValue(target) as IHalconDeserializable)?.Load(item);
                            }

                            continue;
                        }

                        if (type.IsSubclassOf(typeof(Endpoint)))
                        {
                            if (value[1] == 1)
                            {
                                ((infos[index].GetValue(target) as Endpoint).Editor as IHalconDeserializable)?.Load(item);
                            }                          
                            continue;
                        }

                        if (typeof(IHalconDeserializable).IsAssignableFrom(type))
                        {
                            if (value[1] == 1)
                            {
                                (infos[index].GetValue(target) as IHalconDeserializable)?.Load(item);
                            }

                            continue;
                        }

                        if (type == typeof(bool))
                        {
                            if (infos[index].CanWrite)
                                infos[index].SetValue(target, value[1] == 1);
                            continue;
                        }
                        if (type == typeof(TimeSpan))
                        {
                            infos[index].SetValue(target, TimeSpan.ParseExact(value[1], @"hh\:mm\:ss", null));
                            continue;
                        }
                        if (type == typeof(DateTime))
                        {
                            infos[index].SetValue(target, DateTime.FromBinary(long.Parse(value[1].S)));
                            continue;
                        }
                        //if (type == typeof(ImageChannel))
                        //{
                        //    infos[index].SetValue(target, (ImageChannel)((int)value[1]));
                        //    continue;
                        //}
                        //if (type == typeof(DisplayQuality))
                        //{
                        //    infos[index].SetValue(target, (DisplayQuality)((int)value[1]));
                        //    continue;
                        //}
                        if (type == typeof(HTuple))
                        {
                            infos[index].SetValue(target, (HTuple)value[1]);
                            continue;
                        }
                        //if (type == typeof(RsLogixTag))
                        //{
                        //    var result = new RsLogixTag();
                        //    result.Load(item);
                        //    infos[index].SetValue(target, result);
                        //    continue;
                        //}
                        if (type == typeof(double))
                        {
                            infos[index].SetValue(target, value[1].D);
                            continue;
                        }
                        if (type == typeof(int))
                        {
                            infos[index].SetValue(target, value[1].I);
                            continue;
                        }
                        if (type == typeof(string))
                        {
                            infos[index].SetValue(target, value[1].S);
                            continue;
                        }
                        if (type == typeof(HClassMlp))
                        {
                            if (value[1] == 1) infos[index].SetValue(target, item.DeserializeClassMlp());
                            continue;
                        }
                        if (type == typeof(HFunction1D))
                        {
                            infos[index].SetValue(target, new HFunction1D(item.DeserializeTuple()));
                            continue;
                        }
                        if (type == typeof(HShapeModel))
                        {
                            if (value[1] == 1) infos[index].SetValue(target, item.DeserializeShapeModel());
                            continue;
                        }
                        if (type == typeof(HMeasure))
                        {
                            if (value[1] == 1) infos[index].SetValue(target, item.DeserializeMeasure());
                            continue;
                        }
                        if (type == typeof(HVariationModel))
                        {
                            if (value[1] == 1) infos[index].SetValue(target, item.DeserializeVariationModel());
                            continue;
                        }
                        if (type == typeof(HTextureInspectionModel))
                        {
                            if (value[1] == 1) infos[index].SetValue(target, item.DeserializeTextureModel());
                            continue;
                        }

                        if (type == typeof(CollectionOfregion))
                        {
                            (infos[index].GetValue(target) as CollectionOfregion).Load(item);
                            continue;
                        }
                        if (type == typeof(ObservableCollection<IGraphic>))
                        {
                            int count = value[1];
                            ObservableCollection<IGraphic> loaded_collection = (infos[index].GetValue(target) as ObservableCollection<IGraphic>);
                            for (int i = 0; i < count; i++)
                            {
                                string graphic_type = item.DeserializeTuple();
                                IGraphic added = null;
                                switch (graphic_type)
                                {
                                    case "Text": added = new HalconGraphicText(); added.Load(item); break;
                                    case "Line": added = new HalconGraphicLine(); added.Load(item); break;
                                    case "Circle": added = new HalconGraphicCircle(); added.Load(item); break;
                                }
                                if (added != null)
                                    loaded_collection.Add(added);
                            }

                            continue;
                        }
                        if (type == typeof(ObservableCollection<Inputparams>))
                        {
                            int count = value[1];
                            ObservableCollection<Inputparams> loaded_collection = (infos[index].GetValue(target) as ObservableCollection<Inputparams>);
                            for (int i = 0; i < count; i++)
                            {
                                var added = new Inputparams();
                                added.Load(item);
                                loaded_collection.Add(added);
                            }
                            infos[index].SetValue(target, loaded_collection);
                            continue;
                        }

                        //if (type == typeof(ObservableCollection<FilterChainProfile>))
                        //{
                        //    int count = value[1];
                        //    ObservableCollection<FilterChainProfile> loaded_collection = new ObservableCollection<FilterChainProfile>();
                        //    for (int i = 0; i < count; i++)
                        //    {
                        //        FilterChainProfile filterChainProfile = new FilterChainProfile();
                        //        filterChainProfile.Load(item);
                        //        loaded_collection.Add(filterChainProfile);
                        //    }
                        //    infos[index].SetValue(target, loaded_collection);
                        //    continue;
                        //}

                        if (type == typeof(HVariationModel))
                        {
                            if (value[1] == 1) infos[index].SetValue(target, item.DeserializeVariationModel());
                            continue;
                        }

                        if (type == typeof(ObservableCollection<string>))
                        {
                            infos[index].SetValue(target, new ObservableCollection<string>(item.DeserializeTuple().ToSArr()));
                            continue;
                        }
                        if (type == typeof(HClassTrainData))
                        {
                            if (value[1] == 1)
                            {
                                item.serialize_item.FreadSerializedItem(item.file);
                                (infos[index].GetValue(target) as HClassTrainData).DeserializeClassTrainData(item.serialize_item);
                            }
                            continue;
                        }
                        //if (type == typeof(ObservableCollection<ClassID>))
                        //{
                        //    ObservableCollection<ClassID> result = new ObservableCollection<ClassID>();
                        //    for (int i = 0; i < value[1]; i++)
                        //    {
                        //        result.Add(new ClassID(item));
                        //    }
                        //    infos[index].SetValue(target, result);
                        //    continue;
                        //}

                        if (type == typeof(Rect))
                        {
                            //MessageBox.Show("bbb");
                            infos[index].SetValue(target, new Rect(value[1], value[2], value[3], value[4]));
                        }
                        if (type == typeof(NodeType))
                        {
                            //MessageBox.Show("bbb");
                            infos[index].SetValue(target, Enum.Parse(typeof(NodeType),value[1]));
                        }
                        if (type == typeof(ONNXProvider))
                        {
                            //MessageBox.Show("bbb");
                            infos[index].SetValue(target, Enum.Parse(typeof(ONNXProvider), value[1]));
                        }
                        if (type == typeof(HRegion))
                        {
                            infos[index].SetValue(target, item.DeserializeRegion());
                        }
                        if (type == typeof(HImage))
                        {
                            infos[index].SetValue(target, item.DeserializeImage());
                        }
                        if (type == typeof(HNCCModel))
                        {
                            if (value[1] == 1) infos[index].SetValue(target, item.DeserializeNccModel());
                            continue;
                        }
                        if (type == typeof(HBarCode))
                        {
                            if (value[1] == 1) infos[index].SetValue(target, item.DeserializeBarcode());
                            continue;
                        }
                        if (type == typeof(ObservableCollection<IOItem>))
                        {
                            int count = value[1];
                            ObservableCollection<IOItem> loaded_collection = (infos[index].GetValue(target) as ObservableCollection<IOItem>);
                            for (int i = 0; i < count; i++)
                            {
                                var added = new IOItem();
                                added.Load(item);
                                loaded_collection.Add(added);
                            }
                            infos[index].SetValue(target, loaded_collection);
                            continue;
                        }
                        if (type == typeof(List<Type>))
                        {
                            int count = value[1];
                            List<Type> loaded_list = new List<Type>();
                            for (int i = 0; i < count; i++)
                            {
                                var data = item.DeserializeTuple();
                                var asm_name = data.SArr[0];
                                var typename = data.SArr[1];
                                if (typename.Contains("NOVisionDesigner"))
                                {
                                    typename=typename.Replace("NOVisionDesigner", "NOVisionDesigner");
                                }
                                Assembly.Load(asm_name);
                                var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.FullName.Split(',')[0] == asm_name.Split(',')[0]);
                                var t = assembly.GetType(typename);
                                loaded_list.Add(t);
                                //var asm = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == name);
                                //added.Load(item);
                                //loaded_list.Add(added);
                            }
                            infos[index].SetValue(target, loaded_list);
                            continue;
                        }
                        if (type == typeof(ObservableCollection<Type>))
                        {
                            int count = value[1];
                            ObservableCollection<Type> loaded_list = new ObservableCollection<Type>();
                            for (int i = 0; i < count; i++)
                            {
                                var data = item.DeserializeTuple();
                                var asm_name = data.SArr[0];
                                var typename = data.SArr[1];
                                if (typename.Contains("NOVisionDesigner"))
                                {
                                    typename = typename.Replace("NOVisionDesigner", "NOVisionDesigner");
                                }
                                Assembly.Load(asm_name);
                                var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.FullName.Split(',')[0] == asm_name.Split(',')[0]);
                                var t = assembly.GetType(typename);
                                loaded_list.Add(t);
                                //var asm = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == name);
                                //added.Load(item);
                                //loaded_list.Add(added);
                            }
                            infos[index].SetValue(target, loaded_list);
                            continue;
                        }
                        if (type.IsEnum)
                        {
                            infos[index].SetValue(target, Enum.Parse(type, value[1]));
                            continue;
                        }
                        continue;
                    }

                }

            }
        }
        public static void SaveParam(HFile file, object target)
        {
            PropertyInfo[] infos = target.GetType().GetProperties(System.Reflection.BindingFlags.Public
    | System.Reflection.BindingFlags.Instance| System.Reflection.BindingFlags.DeclaredOnly);
            PropertyInfo[] all_infos = target.GetType().GetProperties(System.Reflection.BindingFlags.Public
    | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Default);
            var filtered_infos = new List<PropertyInfo>();
            foreach(var item in all_infos)
            {
                if (!infos.Contains(item))
                {
                    if(Attribute.IsDefined(item, typeof(Serializeable)))
                    {
                        filtered_infos.Add(item);
                    }
                }
                else
                {

                    filtered_infos.Add(item);
                }
            }
            //filtered_infos.AddRange(infos);
            foreach (PropertyInfo info in filtered_infos)
            {
                if (Attribute.IsDefined(info, typeof(SerializeIgnore))){
                    continue;
                }
                string param_name = info.Name;
                object value = info.GetValue(target);
                if (value == null)
                    continue;

                Type type = value.GetType();
                
                if (type.IsGenericType)
                {
                    if(type.GetGenericTypeDefinition()== typeof(ObservableCollection<>))
                    {
                        if (typeof(IHalconDeserializable).IsAssignableFrom(type.GetGenericArguments()[0]))
                        {
                            new HTuple(param_name, (value as System.Collections.ICollection).Count).SerializeTuple().FwriteSerializedItem(file);
                            foreach (IHalconDeserializable item in value as System.Collections.ICollection)
                            {
                                item.Save(file);
                            }
                            continue;
                        }
                    }
                    if (type.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        if (typeof(IHalconDeserializable).IsAssignableFrom(type.GetGenericArguments()[0]))
                        {
                            new HTuple(param_name, (value as System.Collections.ICollection).Count).SerializeTuple().FwriteSerializedItem(file);
                            foreach (IHalconDeserializable item in value as System.Collections.ICollection)
                            {
                                item.Save(file);
                            }
                            continue;
                        }
                    }
                }
                if (typeof(IHalconDeserializable).IsAssignableFrom(type))
                {
                    new HTuple(param_name).TupleConcat(value == null ? 0 : 1).SerializeTuple().FwriteSerializedItem(file);
                    (value as IHalconDeserializable)?.Save(file);
                    continue;
                }
                if (type == typeof(HVariationModel))
                {
                    new HTuple(param_name).TupleConcat(new HTuple((value as HVariationModel).IsInitialized())).SerializeTuple().FwriteSerializedItem(file);
                    if ((value as HVariationModel).IsInitialized())
                    {
                        (value as HVariationModel).SerializeVariationModel().FwriteSerializedItem(file);
                    }
                    continue;
                }
                if (type.IsSubclassOf(typeof(Endpoint)))
                {
                    new HTuple(param_name).TupleConcat((value as Endpoint).Editor == null ? 0 : 1).SerializeTuple().FwriteSerializedItem(file);
                    if((value as Endpoint).Editor == null)
                    {
                        continue;
                    }
                    if(typeof(IHalconDeserializable).IsAssignableFrom((value as Endpoint).Editor.GetType()))
                    {
                        ((value as Endpoint).Editor as IHalconDeserializable).Save(file);
                    }
                    continue;
                }
                if (type == typeof(bool))
                {
                    new HTuple(param_name).TupleConcat(new HTuple((bool)value == true ? 1 : 0)).SerializeTuple().FwriteSerializedItem(file); continue;
                }
                if (type == typeof(Rect))
                {
                    Rect aa = (Rect)value;
                    new HTuple(param_name).TupleConcat(new HTuple(aa.X, aa.Y, aa.Width, aa.Height)).SerializeTuple().FwriteSerializedItem(file); continue;
                }
                if (type == typeof(NodeType))
                {
                    NodeType aa = (NodeType)value;
                    new HTuple(param_name).TupleConcat(aa.ToString()).SerializeTuple().FwriteSerializedItem(file); continue;
                }
                if (type == typeof(ONNXProvider))
                {
                    ONNXProvider aa = (ONNXProvider)value;
                    new HTuple(param_name).TupleConcat(aa.ToString()).SerializeTuple().FwriteSerializedItem(file); continue;
                }
                if (type == typeof(HFunction1D))
                {
                    HFunction1D aa = (HFunction1D)value;
                    new HTuple(param_name).SerializeTuple().FwriteSerializedItem(file);
                    aa.RawData.SerializeTuple().FwriteSerializedItem(file);
                    continue;
                }
                if (type == typeof(TimeSpan))
                {
                    TimeSpan time = (TimeSpan)value;
                    new HTuple(param_name).TupleConcat(new HTuple(time.ToString(@"hh\:mm\:ss"))).SerializeTuple().FwriteSerializedItem(file);

                    continue;
                }
                if (type == typeof(DateTime))
                {
                    DateTime time = (DateTime)value;
                    new HTuple(param_name).TupleConcat(new HTuple(time.ToBinary().ToString())).SerializeTuple().FwriteSerializedItem(file);

                    continue;
                }
                //if (type == typeof(ImageChannel))
                //{
                //    new HTuple(param_name).TupleConcat((int)((ImageChannel)value)).SerializeTuple().FwriteSerializedItem(file);
                //    continue;
                //}
                //if (type == typeof(DisplayQuality))
                //{
                //    new HTuple(param_name).TupleConcat((int)((DisplayQuality)value)).SerializeTuple().FwriteSerializedItem(file);
                //    continue;
                //}
                if (type == typeof(HTuple))
                {
                    new HTuple(param_name).TupleConcat((HTuple)value).SerializeTuple().FwriteSerializedItem(file); continue;
                }
                //if (type == typeof(RsLogixTag))
                //{
                //    new HTuple(param_name).SerializeTuple().FwriteSerializedItem(file);
                //    RsLogixTag aa = (RsLogixTag)value;
                //    aa.Save(file);
                //    continue;
                //}
                if (type == typeof(double))
                {
                    new HTuple(param_name).TupleConcat(new HTuple(value)).SerializeTuple().FwriteSerializedItem(file); continue;
                }

                if (type == typeof(int))
                {
                    new HTuple(param_name).TupleConcat(new HTuple(value)).SerializeTuple().FwriteSerializedItem(file); continue;
                }
                if (type == typeof(string))
                {
                    new HTuple(param_name).TupleConcat(new HTuple(value)).SerializeTuple().FwriteSerializedItem(file); continue;
                }
                if (type == typeof(HClassMlp))
                {

                    new HTuple(param_name).TupleConcat(new HTuple((value as HClassMlp).IsInitialized())).SerializeTuple().FwriteSerializedItem(file);

                    if ((value as HClassMlp).IsInitialized())
                    {
                        (value as HClassMlp).SerializeClassMlp().FwriteSerializedItem(file);
                    }
                    continue;
                }
                if (type == typeof(HShapeModel))
                {

                    new HTuple(param_name).TupleConcat(new HTuple((value as HShapeModel).IsInitialized())).SerializeTuple().FwriteSerializedItem(file);
                    if ((value as HShapeModel).IsInitialized())
                    {
                        (value as HShapeModel).SerializeShapeModel().FwriteSerializedItem(file);
                    }
                    continue;
                }
                if (type == typeof(HMeasure))
                {

                    new HTuple(param_name).TupleConcat(new HTuple((value as HMeasure).IsInitialized())).SerializeTuple().FwriteSerializedItem(file);
                    if ((value as HMeasure).IsInitialized())
                    {
                        (value as HMeasure).SerializeMeasure().FwriteSerializedItem(file);
                    }
                    continue;
                }
                if (type == typeof(HTextureInspectionModel))
                {

                    new HTuple(param_name).TupleConcat(new HTuple((value as HTextureInspectionModel).IsInitialized())).SerializeTuple().FwriteSerializedItem(file);
                    if ((value as HTextureInspectionModel).IsInitialized())
                    {
                        (value as HTextureInspectionModel).SerializeTextureInspectionModel().FwriteSerializedItem(file);
                    }
                    continue;
                }
                if (type == typeof(HBarCode))
                {

                    new HTuple(param_name).TupleConcat(new HTuple((value as HBarCode).IsInitialized())).SerializeTuple().FwriteSerializedItem(file);
                    if ((value as HBarCode).IsInitialized())
                    {
                        (value as HBarCode).SerializeBarCodeModel().FwriteSerializedItem(file);
                    }
                    continue;
                }
                if (type == typeof(HNCCModel))
                {

                    new HTuple(param_name).TupleConcat(new HTuple((value as HNCCModel).IsInitialized())).SerializeTuple().FwriteSerializedItem(file);
                    if ((value as HNCCModel).IsInitialized())
                    {
                        (value as HNCCModel).SerializeNccModel().FwriteSerializedItem(file);
                    }
                    continue;
                }


                if (type == typeof(ObservableCollection<IGraphic>))
                {
                    new HTuple(param_name, (value as ObservableCollection<IGraphic>).Count).SerializeTuple().FwriteSerializedItem(file);
                    foreach (IGraphic _id in value as ObservableCollection<IGraphic>)
                    {
                        _id.Save(file);
                    }
                    continue;
                }
                //if (type == typeof(ObservableCollection<ClassifierClass>))
                //{
                //    new HTuple(param_name, (value as ObservableCollection<ClassifierClass>).Count).SerializeTuple().FwriteSerializedItem(file);
                //    foreach (ClassifierClass _id in value as ObservableCollection<ClassifierClass>)
                //    {
                //        //_id.Save(file);
                //        SaveParam(file, _id);
                //    }
                //    continue;
                //} 

                if (type == typeof(ObservableCollection<Inputparams>))
                {
                    new HTuple(param_name, (value as ObservableCollection<Inputparams>).Count).SerializeTuple().FwriteSerializedItem(file);
                    foreach (Inputparams _id in value as ObservableCollection<Inputparams>)
                    {
                        //_id.Save(file);
                        SaveParam(file, _id);
                    }
                    continue;
                }

                if (type == typeof(HRegion))
                {
                    new HTuple(param_name).SerializeTuple().FwriteSerializedItem(file); (value as HRegion).SerializeRegion().FwriteSerializedItem(file);

                    continue;
                }
                if (type == typeof(HImage))
                {
                    new HTuple(param_name).SerializeTuple().FwriteSerializedItem(file); (value as HImage).SerializeImage().FwriteSerializedItem(file);

                    continue;
                }
                if (type == typeof(CollectionOfregion))
                {

                    new HTuple(param_name).SerializeTuple().FwriteSerializedItem(file);
                    (value as CollectionOfregion).Save(file);
                    continue;
                }
                

                if (type == typeof(HVariationModel))
                {
                    new HTuple(param_name).TupleConcat(new HTuple((value as HVariationModel).IsInitialized())).SerializeTuple().FwriteSerializedItem(file);
                    if ((value as HVariationModel).IsInitialized())
                    {
                        (value as HVariationModel).SerializeVariationModel().FwriteSerializedItem(file);
                    }
                    continue;
                }

                //if (type == typeof(ObservableCollection<ClassID>))
                //{

                //    new HTuple(param_name, (value as ObservableCollection<ClassID>).Count).SerializeTuple().FwriteSerializedItem(file);
                //    foreach (ClassID _id in value as ObservableCollection<ClassID>)
                //    {
                //        _id.Save(file);
                //    }
                //    continue;
                //}


                if (type == typeof(ObservableCollection<string>))
                {

                    new HTuple(param_name).SerializeTuple().FwriteSerializedItem(file);
                    new HTuple((value as ObservableCollection<String>).ToArray<string>()).SerializeTuple().FwriteSerializedItem(file);
                    continue;
                }
                if (type == typeof(HClassTrainData))
                {
                    new HTuple(param_name).TupleConcat(new HTuple((value as HClassTrainData) != null)).SerializeTuple().FwriteSerializedItem(file);
                    if ((value as HClassTrainData) != null)
                    {
                        (value as HClassTrainData).SerializeClassTrainData().FwriteSerializedItem(file);
                    }
                    continue;
                }
                if (type == typeof(ObservableCollection<IOItem>))
                {
                    new HTuple(param_name, (value as ObservableCollection<IOItem>).Count).SerializeTuple().FwriteSerializedItem(file);
                    foreach (IOItem _id in value as ObservableCollection<IOItem>)
                    {
                        //_id.Save(file);
                        SaveParam(file, _id);
                    }
                    continue;
                }
                if (type == typeof(List<Type>))
                {
                    new HTuple(param_name, (value as List<Type>).Count).SerializeTuple().FwriteSerializedItem(file);
                    foreach (Type _id in value as List<Type>)
                    {
                        var asm = _id?.Assembly.FullName;
                        var typename = _id.FullName;
                        new HTuple(asm).TupleConcat(new HTuple(typename)).SerializeTuple().FwriteSerializedItem(file);
                    }
                    continue;
                }
                if (type == typeof(ObservableCollection<Type>))
                {
                    new HTuple(param_name, (value as ObservableCollection<Type>).Count).SerializeTuple().FwriteSerializedItem(file);
                    foreach (Type _id in value as ObservableCollection<Type>)
                    {
                        var asm = _id.Assembly.FullName;
                        var typename = _id.FullName;
                        new HTuple(asm).TupleConcat(new HTuple(typename)).SerializeTuple().FwriteSerializedItem(file);
                    }
                    continue;
                }
                if (type.IsEnum)
                {
                    new HTuple(param_name, (value.ToString())).SerializeTuple().FwriteSerializedItem(file);
                    continue;
                }
                new HTuple("null").TupleConcat(new HTuple(0)).SerializeTuple().FwriteSerializedItem(file); continue;


            }
            new HTuple("EndSaveClass").SerializeTuple().FwriteSerializedItem(file);
        }
    }
    
}
