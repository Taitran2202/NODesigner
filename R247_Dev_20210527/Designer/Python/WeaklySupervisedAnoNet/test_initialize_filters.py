from config import *
from initialize_filter import *

F = make_lm_filter(SUP=49)
f, axarr = plt.subplots(6, 8, figsize = (20 , 20))
for i in range(6):
  axarr[i][0].imshow(F[:,:,i * 8], cmap='gray')
  axarr[i][1].imshow(F[:,:,i * 8 + 1], cmap="gray")
  axarr[i][2].imshow(F[:,:,i * 8 + 2], cmap="gray")
  axarr[i][3].imshow(F[:,:,i * 8 + 3], cmap="gray")
  axarr[i][4].imshow(F[:,:,i * 8 + 4], cmap="gray")
  axarr[i][5].imshow(F[:,:,i * 8 + 5], cmap='gray')
  axarr[i][6].imshow(F[:,:,i * 8 + 6], cmap="gray")
  axarr[i][7].imshow(F[:,:,i * 8 + 7], cmap="gray")
plt.show()
plt.savefig('./Results/evol_conc_v'+str(vinit)+'a_'+str(a)+'.png')

F = make_rfs_filter(SUP=49)
f, axarr = plt.subplots(5, 8, figsize = (20 , 20))
for i in range(5):
  axarr[i][0].imshow(F[:,:,i * 8], cmap='gray')
  axarr[i][1].imshow(F[:,:,i * 8 + 1], cmap="gray")
  axarr[i][2].imshow(F[:,:,i * 8 + 2], cmap="gray")
  axarr[i][3].imshow(F[:,:,i * 8 + 3], cmap="gray")
  axarr[i][4].imshow(F[:,:,i * 8 + 4], cmap="gray")
  axarr[i][5].imshow(F[:,:,i * 8 + 5], cmap='gray')
  if i != 4:
    axarr[i][6].imshow(F[:,:,i * 8 + 6], cmap="gray")
    axarr[i][7].imshow(F[:,:,i * 8 + 7], cmap="gray")
plt.show()
plt.savefig('./Results/evol_conc_v'+str(vinit)+'a_'+str(a)+'.png')


F = make_s_filter(SUP=49)
f, axarr = plt.subplots(3, 5, figsize = (50 , 50))
for i in range(3):
  axarr[i][0].imshow(F[:,:,i * 5], cmap='gray')
  axarr[i][0].imshow(F[:,:,i * 5 + 1], cmap='gray')
  axarr[i][0].imshow(F[:,:,i * 5 + 2], cmap='gray')
  if i != 2:
    axarr[i][0].imshow(F[:,:,i * 5 + 3], cmap='gray')
    axarr[i][0].imshow(F[:,:,i * 5 + 4], cmap='gray')
plt.show()
plt.savefig('./Results/evol_conc_v'+str(vinit)+'a_'+str(a)+'.png')