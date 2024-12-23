from config import *

# LM filters

def make_lm_filter(SUP=49):
  def gaussian1d(sigma, mean, x, ord):
    x = np.array(x)
    x_ = x - mean
    var = sigma**2

    # Gaussian Function
    g1 = (1/np.sqrt(2*np.pi*var))*(np.exp((-1*x_*x_)/(2*var)))

    if ord == 0:
        g = g1
        return g
    elif ord == 1:
        g = -g1*((x_)/(var))
        return g
    else:
        g = g1*(((x_*x_) - var)/(var**2))
        return g

  def gaussian2d(sup, scales):
    var = scales * scales
    shape = (sup,sup)
    n,m = [(i - 1)/2 for i in shape]
    x,y = np.ogrid[-m:m+1,-n:n+1]
    g = (1/np.sqrt(2*np.pi*var))*np.exp( -(x*x + y*y) / (2*var) )
    return g

  def log2d(sup, scales):
    var = scales * scales
    shape = (sup,sup)
    n,m = [(i - 1)/2 for i in shape]
    x,y = np.ogrid[-m:m+1,-n:n+1]
    g = (1/np.sqrt(2*np.pi*var))*np.exp( -(x*x + y*y) / (2*var) )
    h = g*((x*x + y*y) - var)/(var**2)
    return h

  def makefilter(scale, phasex, phasey, pts, sup):
    gx = gaussian1d(3*scale, 0, pts[0,...], phasex)
    gy = gaussian1d(scale,   0, pts[1,...], phasey)

    image = gx*gy

    image = np.reshape(image,(sup,sup))
    return image
  
  sup     = SUP
  scalex  = np.sqrt(2) * np.array([1,2,3])
  norient = 6
  nrotinv = 12

  nbar  = len(scalex)*norient
  nedge = len(scalex)*norient
  nf    = nbar+nedge+nrotinv
  F     = np.zeros([sup,sup,nf])
  hsup  = (sup - 1)/2

  x = [np.arange(-hsup,hsup+1)]
  y = [np.arange(-hsup,hsup+1)]

  [x,y] = np.meshgrid(x,y)

  orgpts = [x.flatten(), y.flatten()]
  orgpts = np.array(orgpts)

  count = 0
  for scale in range(len(scalex)):
      for orient in range(norient):
          angle = (np.pi * orient)/norient
          c = np.cos(angle)
          s = np.sin(angle)
          rotpts = [[c+0,-s+0],[s+0,c+0]]
          rotpts = np.array(rotpts)
          rotpts = np.dot(rotpts,orgpts)
          F[:,:,count] = makefilter(scalex[scale], 0, 1, rotpts, sup)
          F[:,:,count+nedge] = makefilter(scalex[scale], 0, 2, rotpts, sup)
          count = count + 1

  count = nbar+nedge
  scales = np.sqrt(2) * np.array([1,2,3,4])

  for i in range(len(scales)):
      F[:,:,count]   = gaussian2d(sup, scales[i])
      count = count + 1

  for i in range(len(scales)):
      F[:,:,count] = log2d(sup, scales[i])
      count = count + 1

  for i in range(len(scales)):
      F[:,:,count] = log2d(sup, 3*scales[i])
      count = count + 1

  return F



# RFS filters
def make_rfs_filter(SUP=49):
  
  def makefilter(scale, phasex, phasey, pts, sup):
    gx = gauss1d(3 * scale, 0, pts[0, :], phasex)
    gy = gauss1d(scale, 0, pts[1, :], phasey)
    f = normalise((gx * gy).reshape((sup, sup)))
    return f

  def gauss1d(sigma, mean, x, ord):
    x = x - mean;
    num = x * x;
    variance = sigma**2;
    denom = 2 * variance; 
    g = np.exp(-num / denom) / np.sqrt(np.pi * denom)
    
    if ord == 0:
        return g
    elif ord == 1:
        return -g * (x / variance)
    else:
        return g * ((num - variance) / variance**2);

  def normalise(f):
    f = f - np.mean(f);
    f = f / sum(abs(f));
    return f

  sup = SUP
  scalex = [1, 2, 4]
  norient = 6
  nrotinv = 2
  
  nbar = len(scalex) * norient
  nedge = len(scalex) * norient
  nf = nbar + nedge + nrotinv
  F = np.zeros((sup, sup, nf))
  hsup = (sup - 1) / 2

  x, y = np.meshgrid(np.arange(-hsup, hsup + 1), np.arange(hsup, -hsup - 1, -1))
  
  orgpts = np.array([x.flatten(), y.flatten()])

  count = 0;
  for scale in scalex:
      for orient in range(norient):
          angle = np.pi * orient / norient
          c = np.cos(angle)
          s = np.sin(angle)
          
          rotpts = np.array([[c, -s], [s, c]]).dot(orgpts)
          
          F[:, :, count] = makefilter(scale, 0, 1, rotpts, sup);
          F[:, :, count + nedge] = makefilter(scale, 0, 2, rotpts, sup);
          
          count=count+1;

  r = orgpts[0, :]**2 + orgpts[1, :]**2
  sigma = 10.0
  
  F[:, :, nbar + nedge] = normalise(((1.0 / np.sqrt(2 * np.pi * sigma**2)) * np.exp(-r / (2 * sigma**2))).reshape(sup, sup))
  F[:, :, nbar + nedge + 1] = normalise(((r - 2 * sigma**2) / (sigma ** 4) * np.exp(-r / (2 * sigma**2))).reshape(sup, sup))

  return F



# S filters

def make_s_filter(NF=13, SUP=7):
    def make_filter(sup, sigma, tau):
        hsup = int((sup-1) / 2)
        grid = [i for i in range(-hsup, hsup + 1)]
        [x,y]=np.meshgrid(grid, grid)
        r = (x*x + y*y)**0.5
        f = np.cos(r*(math.pi*tau/sigma))*np.exp(-(r*r)/(2*sigma*sigma))
        f = f-np.mean(f)
        f = f / np.sum(np.abs(f))
        return f
    F=np.zeros(shape=(SUP,SUP,NF))
    F[:,:,0] = make_filter(SUP,2,1)
    F[:,:,1] = make_filter(SUP,4,1)
    F[:,:,2] = make_filter(SUP,4,2)
    F[:,:,3] = make_filter(SUP,6,1)
    F[:,:,4] = make_filter(SUP,6,2)
    F[:,:,5] = make_filter(SUP,6,3)
    F[:,:,6] = make_filter(SUP,8,1)
    F[:,:,7] = make_filter(SUP,8,2)
    F[:,:,8] = make_filter(SUP,8,3)
    F[:,:,9] = make_filter(SUP,10,1)
    F[:,:,10] = make_filter(SUP,10,2)
    F[:,:,11] = make_filter(SUP,10,3)
    F[:,:,12] = make_filter(SUP,10,4)
    return F