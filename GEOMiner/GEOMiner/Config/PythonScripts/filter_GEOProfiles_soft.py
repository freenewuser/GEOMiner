import argparse
import os
import codecs


parser = argparse.ArgumentParser()
parser.add_argument("path", type=str,
                    help="path to txt file to be read")
parser.add_argument("destination_identifiers", type=str,
                    help="path where to write the table legend")
parser.add_argument("destination_table", type=str,
                    help="path where to write the data")
args = parser.parse_args()
args = args.__dict__

path = args["path"]
dest_ident = args["destination_identifiers"]
dest_table = args["destination_table"]



try:
    f = codecs.open(path,'r', encoding="utf-8")
    try:
        if os.path.isfile(dest_ident): os.remove(dest_ident)
        g = codecs.open(dest_ident,'a', encoding="utf-8")
        if os.path.isfile(dest_table): os.remove(dest_table)
        h = codecs.open(dest_table,'a', encoding="utf-8")
        n = 0
        try: 
            matrix = iter(f)
            line = next(matrix)
            while (not line.startswith("!dataset_table_begin")):
                g.write(line[1:].replace(';',',').replace("\t",";"))
                line = next(matrix)
            line = next(matrix)
            while (not line.startswith("!dataset_table_end")):
                h.write(line.replace(';',',').replace("\t",";"))
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
        h.close()
except Exception as exc: 
    print("Error: could not read file at {0}".format(path))
    print(exc)
finally:
    f.close() 

print(','.join([dest_ident,dest_table]))