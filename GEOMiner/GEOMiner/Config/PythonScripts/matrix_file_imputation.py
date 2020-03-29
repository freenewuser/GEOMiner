import argparse
import os

parser = argparse.ArgumentParser()
parser.add_argument("path", type=str,
                    help="path to txt file to be read")
parser.add_argument("destination", type=str,
                    help="path where to write result")
parser.add_argument("imputation_paths", type=str,
                    help="comma separated paths to files with supplementary data")
args = parser.parse_args()
args = args.__dict__

path = args["path"]
dest = args["destination"]

import codecs

try:
    f = codecs.open(path,'r',encoding='utf-8')
    try:
        if os.path.isfile(dest): os.remove(dest)
        g = codecs.open(dest,'a',encoding='utf-8')
        for line in f:
            g.write(line)
    except: 
        print("Error: could not open or create destination file at {0}".format(dest))
    finally:
        g.close()
except: 
    print("Error: could not read file at {0}".format(path))
finally:
    f.close()

print(dest)
