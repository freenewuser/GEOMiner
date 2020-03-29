import argparse
parser = argparse.ArgumentParser()
parser.add_argument("path", type=str,
                    help="path to txt file to be read")
args = parser.parse_args()
args = args.__dict__


path = args['path']

from collections import Counter

def y_lines(file):
    n=1
    for line in file:
        if n>32: break;
        if line != "":
            n+=1
            yield line

import codecs

try:
    f = codecs.open(path,'r',encoding='utf-8')
    lines = [line for line in y_lines(f)]
except Exception as exc:
    print('Error: could not open file at {0}'.format(path))
    print(exc)
finally:
    f.close()

try:
    counts = [set(Counter(i).items()) for i in lines]
    counts2 = [tuple(c) for c in counts]

    if len(counts)<3: print("Error") #return "Error: Matrix too short or too many free lines"
        
    it = iter(counts)
    counts = [a.intersection(b) for (a,b) in zip(it,it)]

    it = iter(counts)
    counts = [a.intersection(b) for (a,b) in zip(it,it)]

    counts = Counter([tuple(elem for elem in line if not elem[0].isalnum() and elem[0]!='.') for line in counts])
    delimiters = counts.most_common(1)[0][0]

    line_delimiter = [delim for (delim,num) in delimiters if num==1][0]

    lims = [delim for (delim,num) in delimiters if num>1]
    regex = '['+'|'.join(lims) +']+'

    import re

    item_delimiter = [re.finditer(regex,line) for line in lines]
    temp = [tuple(Counter([limiter.group() for limiter in line]).items()) for line in item_delimiter]
    temp = [t for t in temp if len(t)>0]
    item_delimiter = Counter(temp)

    max_delim_length = max([len(counts[0][0][0]) for counts in item_delimiter.most_common()])

    for counts in item_delimiter.most_common():
        if len(counts[0][0][0]) == max_delim_length:
            item_delimiter = counts[0][0][0]
            
    print( r"'{0}' '{1}'".format(item_delimiter,line_delimiter) )
except Exception as exc:
    print("Error: could not process file at {0}".format(path))
    print(exc)