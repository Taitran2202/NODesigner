# NOVision Designer
## 1. Devexpress setup:
 - Login devexpress: https://www.devexpress.com/MyAccount/LogIn/?returnUrl=https%3A%2F%2Fwww.devexpress.com%2F
 - Username: 
   ```
   toan.do@new-ocean.com.vn
   ```
 - Password: 
   ```
   new-ocean
   ```
 - Select devepxress WPF version 19.2.9 and download.
 - Install devexpress WPF 19.2.9 to this location: D:\ProgramFiles\DevExpress 19.2.
 - Select registerd product installation when asking by installer and log in with username + password above.
## 2. Install dependencies:
 - OpenCV Cuda (for opticalflow using GPU):
   ```
   mkdir C:\src\cudaopticalflow\cudaopticalflowtest\OpticalFlowCudaCV\bin\Release & curl -o C:\src\cudaopticalflow\cudaopticalflowtest\OpticalFlowCudaCV\bin\Release\OpticalFlowCudaCV.dll https://f004.backblazeb2.com/file/NOVisionDesigner/deps/OpticalFlowCudaCV.dll
   ```
 - NOVision Python (for training) follow instruction at : https://github.com/namtr92/novision
## 3. Cuda + python3.8 setup:
 - Tensorflow-gpu version 2.8.0:
   ```
   pip install tensorflow-gpu==2.8.0
   ```
 - Pytorch 1.12.1 cuda: 
   ```
   pip install torch==1.12.1+cu113 torchvision==0.13.1+cu113 torchaudio==0.12.1 --extra-index-url https://download.pytorch.org/whl/cu113
   ```
 - Cuda Toolkit 11.4: https://developer.download.nvidia.com/compute/cuda/11.4.4/network_installers/cuda_11.4.4_windows_network.exe
 - Cudnn v8.2.2 for cuda 11.4: https://developer.nvidia.com/compute/machine-learning/cudnn/secure/8.2.2/11.4_07062021/cudnn-11.4-windows-x64-v8.2.2.26.zip
