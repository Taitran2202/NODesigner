using Microsoft.ML.OnnxRuntime;
using NodeNetwork.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Nodes
{
    public enum ModelState
    {
        Loaded, Error, Unloaded,NotFound,Loading
    }
    public enum ModelLoadType
    {
        SavedModel, FrozenGraph,ONNX
    }
    public class ONNXModel
    {
        [SerializeIgnore]
        public ModelState State { get; set; } = ModelState.Unloaded;
        [Browsable(true)]
        [Serializeable]
        public ONNXProvider Provider { get; set; } = ONNXProvider.CUDA;
        public SessionOptions CreateProviderOption(string directory)
        {
            SessionOptions options = null;
            switch (Provider)
            {
                case ONNXProvider.TensorRT:
                    var ortTensorRTProviderOptions = new OrtTensorRTProviderOptions();
                    ortTensorRTProviderOptions.UpdateOptions(new Dictionary<string, string>()
                      {
                          // Enable INT8 for QAT models, disable otherwise.
                          { "trt_fp16_enable", "1" },
                       // {"trt_int8_use_native_calibration_table","1" },
                        //{"trt_int8_enable", "1"  },
                        //{"trt_int8_use_native_calibration_table","1" },
                          { "trt_engine_cache_enable", "1" },
                          {"trt_engine_cache_path",directory},
                     });
                    options = SessionOptions.MakeSessionOptionWithTensorrtProvider(ortTensorRTProviderOptions);
                    
                    break;
                case ONNXProvider.CUDA:
                    OrtCUDAProviderOptions cudaOption = new OrtCUDAProviderOptions();
                    cudaOption.UpdateOptions(new Dictionary<string, string>()
                      {
                           //{"cudnn_conv_algo_search","HEURISTIC"},
                            {"device_id","0" },
                        //{"cudnn_conv_use_max_workspace","0" }
                            {"arena_extend_strategy", "kSameAsRequested" }
                     });
                    options = SessionOptions.MakeSessionOptionWithCudaProvider(cudaOption);
                    options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_DISABLE_ALL;
                    break;
                case ONNXProvider.CPU:
                    options = new SessionOptions();
                    break;
            }
            return options;
        }
        public virtual bool LoadRecipe()
        {
            return false;
        }
        public virtual bool ContinuousLoadRecipe()
        {
            return false;
        }
    }
}
