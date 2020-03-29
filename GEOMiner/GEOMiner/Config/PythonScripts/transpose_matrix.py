import argparse
parser = argparse.ArgumentParser()
parser.add_argument("path", type=str,
                    help="path to csv file to be read")
#parser.add_argument("destination", type=str,
#                    help="path for the csv file to be written to")
args = parser.parse_args()
args = args.__dict__

path = args['path']

try:
    import os
    import codecs
    import re

    alt = path.replace(".csv","_old.csv")
    dest = path
    os.rename(path,alt)

    try:
        f = codecs.open(alt,'r',encoding="utf-8")
        matrix = [line.split(';') for line in re.sub("[\r\n]+","\n",f.read()).split('\n') if len(line)>1]
        matrix = zip(*matrix)
    except Exception as exc:
        print("Error: could not open file at {0}".format(path))
        print(exc)
    finally:
        f.close()
        os.remove(alt)

    try:
        if os.path.isfile(dest): os.remove(dest)        
        g = codecs.open(dest,'a',encoding="utf-8")
        for line in matrix:
            g.write(';'.join(line)+'\n')
    except Exception as exc:
        print("Error: could not write file at {0}".format(dest))
        print(exc)
    finally: 
        g.close()
    print(path)
except Exception as exc:
    print("Error:", exc)
