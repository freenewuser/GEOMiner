import argparse
import os
import codecs


parser = argparse.ArgumentParser()
parser.add_argument("path", type=str,
                    help="path to txt file to be read")
parser.add_argument("destination", type=str,
                    help="path where to write result")
args = parser.parse_args()
args = args.__dict__

path = args["path"]
dest = args["destination"]


try:
    f = codecs.open(path,'r', encoding="utf-8")
    try:
        if os.path.isfile(dest): os.remove(dest)
        g = codecs.open(dest,'a', encoding="utf-8")
        n = 0
        try: 
            matrix = iter(f)
            line = next(matrix)
            while (not line.startswith("!series_matrix_table_begin")):
                if (line.startswith('!Sample')): g.write(line[1:].replace(';',',').replace("\t",";"))
                line = next(matrix)
            line = next(matrix)
            while (not line.startswith("!series_matrix_table_end")):
                g.write(line.replace(';',',').replace("\t",";"))
                line = next(matrix)
                n+=1
        except Exception as exc:
            print("Error: could not complete writing operation from {0} to {1}".format(path,dest))
            print(exc)
    except Exception as exc: 
        print("Error: could not open or create destination file at {0}".format(dest))
        print(exc)
    finally:
        g.close()
except Exception as exc: 
    print("Error: could not read file at {0}".format(path))
    print(exc)
finally:
    f.close() 

if n==1:
    import re
    try:
        g = codecs.open(dest,'r', encoding="utf-8")
        for line in g:
            if "GSM" in line:
                print(','.join(re.findall('\"(GSM\d+?)\"',line)))
                break
    except Exception as exc: 
        print("Error: could not open destination file")
        print(exc)
    finally: g.close()
else: print(dest)