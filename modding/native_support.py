"""Install the original Mono native helpers in Android's native library namespace."""
import io,struct,zipfile,hashlib
HELPERS=('libmono-native.so','libMonoPosixHelper.so','libmono-btls-shared.so','libmono-ee-interp.so','libMonoSupportW.so')
def elf_symbols(data):
 if data[:6]!=b'\x7fELF\x02\x01' or struct.unpack_from('<H',data,18)[0]!=183:raise ValueError('Expected little-endian ARM64 ELF')
 offset=struct.unpack_from('<Q',data,40)[0];size,count=struct.unpack_from('<HH',data,58)
 sections=[struct.unpack_from('<IIQQQQIIQQ',data,offset+i*size) for i in range(count)]
 names=set()
 for section in sections:
  if section[1]!=11:continue
  strings=sections[section[6]];table=data[strings[4]:strings[4]+strings[5]]
  for p in range(section[4],section[4]+section[5],section[9]):
   name,info,other,index,value,length=struct.unpack_from('<IBBHQQ',data,p)
   if index and name:names.add(table[name:table.find(b'\0',name)].decode())
 return names

def helpers(apk):
 with zipfile.ZipFile(io.BytesIO(apk.read('assets/Mono/mono.zip'))) as mono:
  result={name:mono.read('mono/lib/'+name) for name in HELPERS}
 for data in result.values():elf_symbols(data)
 if 'SystemNative_Stat2' not in elf_symbols(result['libmono-native.so']):raise ValueError('Missing native filesystem API')
 if 'mono_dllmap_insert' not in elf_symbols(apk.read('lib/arm64-v8a/libmonosgen-2.0.so')):raise ValueError('Mono lacks dllmap bootstrap API')
 return result

def verify(apk_path,source_path):
 with zipfile.ZipFile(source_path) as source,zipfile.ZipFile(apk_path) as apk:
  original=helpers(source)
  for name,data in original.items():
   if apk.read('lib/arm64-v8a/'+name)!=data:raise ValueError('Native helper mismatch: '+name)
 return {name:hashlib.sha256(data).hexdigest() for name,data in original.items()}
