"""Keep the exact source manifest except two explicit installation settings."""
import struct

def attributes(data):
    b=data
    if struct.unpack_from('<HHI',b,0)!=(3,8,len(b)):
        raise ValueError('Invalid binary Android manifest header')
    kind,header,size=struct.unpack_from('<HHI',b,8)
    if kind!=1 or header!=28:raise ValueError('Unexpected manifest string pool')
    count=struct.unpack_from('<I',b,16)[0]
    flags=struct.unpack_from('<I',b,24)[0]
    if flags&0x100:raise ValueError('Expected source UTF-16 pool')
    start=8+struct.unpack_from('<I',b,28)[0]
    strings=[]
    for i in range(count):
        p=start+struct.unpack_from('<I',b,36+4*i)[0]
        length=struct.unpack_from('<H',b,p)[0]
        if length&0x8000:raise ValueError('Unexpected long manifest string')
        strings.append(bytes(b[p+2:p+2+2*length]).decode('utf-16le'))
    offset=8
    while offset<len(b):
        kind,header,size=struct.unpack_from('<HHI',b,offset)
        if size<header or size==0 or offset+size>len(b):raise ValueError('Invalid manifest chunk')
        if kind==0x102:
            element=strings[struct.unpack_from('<I',b,offset+20)[0]]
            attr_start,attr_size,attr_count=struct.unpack_from('<HHH',b,offset+24)
            if attr_size!=20:raise ValueError('Unexpected attribute size')
            for j in range(attr_count):
                a=offset+16+attr_start+j*attr_size
                name=strings[struct.unpack_from('<I',b,a+4)[0]]
                typed=struct.unpack_from('<I',b,a+12)[0]>>24
                value=struct.unpack_from('<I',b,a+16)[0]
                yield element,name,typed,value,a+16
        offset+=size

def prepare_manifest(data):
    b=bytearray(data);changed=[]
    for element,name,typed,value,offset in attributes(data):
        if (element,name) in [('manifest','installLocation'),('application','debuggable')]:
            if (name,typed,value) not in [('installLocation',16,2),('debuggable',18,0xffffffff)]:
                raise ValueError('Unexpected source installation attribute')
            struct.pack_into('<I',b,offset,0);changed.append((element,name))
    if len(changed)!=2:raise ValueError('Missing installation attributes')
    return bytes(b)
