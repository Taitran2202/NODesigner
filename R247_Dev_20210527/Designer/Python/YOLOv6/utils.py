from config import *


def allowed_file(filename):
    return ("." in filename and filename.rsplit(".", 1)[1].lower() in ["png", "jpg", "jpeg"])

def read_class_names(class_file_name):
    names = {}
    for ID, data in enumerate(class_file_name):
        names[ID] = data.Name
    return names

def load_annotation_json(file_paths):
    label_dict = {}
    boxes = []
    image_names = []
    width = 0
    height = 0
    for file_path  in file_paths:
        with open(file_path, 'r') as f:
            data = json.load(f)
            width = data['imageWidth']
            height = data['imageHeight']
            image_names.append(data['imagePath'].split('\\')[-1])
            box = []
            for idx in range(len(data['shapes'])):
                label_name = data['shapes'][idx]['label']
                if not label_name in list(label_dict.keys()):
                    values = list(label_dict.values())
                    if len(values) == 0:
                        label_dict[label_name] = 0
                    else:
                        label_dict[label_name] = values[-1] + 1
                
                points = data['shapes'][idx]['points']
                x1, y1 = points[0]
                x2, y2 = points[1]
                x3, y3 = points[2]
                x4, y4 = points[3]
                box.append([x1, y1, x2, y2, x3, y3, x4, y4, label_dict[label_name]])
        boxes.append(np.array(box))
    return label_dict, image_names, np.array(boxes), width, height


def load_annotation_xml(file_paths):
    label_dict = {}
    boxes = []
    image_names = []
    width = 0
    height = 0
    for file_path in file_paths:
        tree = ET.parse(file_path)
        root = tree.getroot()
        file_name = root.find('filename').text
        image_names.append(file_name)
        size = root.find('size')
        width = int(size.find('width').text)
        height = int(size.find('height').text)
        box = []
        for object in root.findall('object'):
            label_name = object.find('name').text
            x = object.find('robndbox').find('cx').text
            y = object.find('robndbox').find('cy').text
            w = object.find('robndbox').find('w').text
            h = object.find('robndbox').find('h').text
            a = float(object.find('robndbox').find('angle').text)
            while a > (np.pi / 2):
                a -= np.pi
            while a < (-np.pi / 2):
                a += np.pi
            if not label_name in list(label_dict.keys()):
                values = list(label_dict.values())
                if len(values) == 0:
                    label_dict[label_name] = 0
                else:
                    label_dict[label_name] = values[-1] + 1
            
            box.append([x, y, w, h, a, label_dict[label_name]])
        boxes.append(np.array(box, dtype=np.float32))
    return label_dict, boxes, image_names, width, height


def get_image_paths(image_dir):
    folders = [str(f) for f in Path(image_dir).glob("*")]
    image_paths = []
    if folders:
        if allowed_file(folders[0]):
            image_paths = folders
            return image_paths
        else:
            for idx, folder in enumerate(folders):
                for file in os.listdir(folder):
                    filename = os.fsdecode(file)
                    if allowed_file(filename):
                        image_paths.append(os.path.join(folder, filename))
            return image_paths