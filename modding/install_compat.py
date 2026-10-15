"""Installation settings used by the device-confirmed compatibility build."""
import struct


def prepare_manifest(data):
    b = bytearray(data)
    count = struct.unpack_from('<I', b, 16)[0]
    flags = struct.unpack_from('<I', b, 24)[0]
    if flags & 0x100:
        raise ValueError('Expected the verified source manifest UTF-16 string pool')
    start = 8 + struct.unpack_from('<I', b, 28)[0]
    strings = []
    for i in range(count):
        p = start + struct.unpack_from('<I', b, 36 + 4*i)[0]
        size = struct.unpack_from('<H', b, p)[0]
        strings.append(bytes(b[p+2:p+2+2*size]).decode('utf-16le'))
    offset = 8
    changed = []
    while offset < len(b):
        kind, header, size = struct.unpack_from('<HHI', b, offset)
        if size < header or offset + size > len(b):
            raise ValueError('Invalid manifest chunk')
        if kind == 0x102:
            element = strings[struct.unpack_from('<I', b, offset+20)[0]]
            attr_start, attr_size, attr_count = struct.unpack_from('<HHH', b, offset+24)
            for j in range(attr_count):
                a = offset + 16 + attr_start + j*attr_size
                name = strings[struct.unpack_from('<I', b, a+4)[0]]
                if (element, name) in [('manifest', 'installLocation'), ('application', 'debuggable')]:
                    struct.pack_into('<I', b, a+16, 0)
                    changed.append((element, name))
        offset += size
    if len(changed) != 2:
        raise ValueError('Missing expected installation attributes')
    return bytes(b)
