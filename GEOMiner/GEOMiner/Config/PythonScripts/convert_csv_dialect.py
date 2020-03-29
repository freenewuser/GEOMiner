import argparse
parser = argparse.ArgumentParser()
parser.add_argument("path", type=str,
                    help="path to txt file to be read")
parser.add_argument("item_delimiter", type=str,
                    help="delimiting  characters between items")
parser.add_argument("line_delimiter", type=str,
                    help="delimiting  characters between lines")
parser.add_argument("dest", type=str,
                    help="path to txt file to be written to")
args = parser.parse_args()
args = args.__dict__

path = args["path"]
item_delimiter = args["item_delimiter"]
line_delimiter = args["line_delimiter"]
dest = args["dest"]

import os
oldpath = path
if path==dest: 
    oldpath = path
    path = path.replace('.txt','_old.txt')
    try:
        os.rename(oldpath,path)
    except Exception as exc: 
        print("Error: could not read file at {0}".format(oldpath))
        print(exc)

import codecs

try:
    f = codecs.open(path,'r',encoding='utf-8')
except Exception as exc: 
    print("Error: could not read file at {0}".format(path))
    print(exc)
finally: f.close()


try:
    if os.path.isfile(dest): os.remove(dest)
    g = codecs.open(dest,'a',encoding='utf-8')
except Exception as exc:
    print("Error: could open or create destination file at {0}".format(dest))
    print(exc)

try:
    for line in f:
        g.write(line.replace(';',',').replace(item_delimiter,';')+'\n')
except Exception as exc:
    print("Error: could not write destination file at {0}".format(dest))
    print(exc)
finally:
    g.close()

try: f.close()
except: None

try:
    if oldpath!=path: os.remove(oldpath)
except Exception as exc: 
    print("Error: failed deleting file at {0}".format(dest))
    print(exc)

print(dest)