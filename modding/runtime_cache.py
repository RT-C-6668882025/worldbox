"""Version the native Mono extraction directory; never delete user save data."""
import base64,hashlib,json
PATTERN=b'{}/mono\0'
def cache_id(assembly,helper):
 return base64.b32encode(hashlib.sha256(assembly+helper+b'GodWorld-runtime-cache-v1').digest()).decode('ascii')[:4].lower()
def patch_bridge(native,identifier):
 replacement=b'{}/'+identifier.encode('ascii')+b'\0'
 if len(replacement)!=len(PATTERN) or not identifier.isalnum():raise ValueError('Invalid cache namespace')
 if native.count(PATTERN)!=1:raise ValueError('Unexpected native Mono cache path; reinspect loader')
 result=native.replace(PATTERN,replacement)
 assert len(result)==len(native)
 return result
def scripting_manifest(raw):
 manifest=json.loads(raw)
 if len(manifest['names'])!=len(manifest['types']):raise ValueError('Mismatched scripting assembly metadata')
 if 'RebellionRules.dll' not in manifest['names']:
  manifest['names'].append('RebellionRules.dll');manifest['types'].append(16)
 return json.dumps(manifest,separators=(',',':')).encode()

def verify_runtime(apk_path,source_path,assembly,helper):
 import zipfile
 identifier=cache_id(assembly,helper)
 with zipfile.ZipFile(source_path) as source,zipfile.ZipFile(apk_path) as apk:
  before=source.read('lib/arm64-v8a/libil2cpp.so');after=apk.read('lib/arm64-v8a/libil2cpp.so')
  if after!=patch_bridge(before,identifier):raise ValueError('Runtime extraction namespace patch differs from verified bridge')
  manifest=json.loads(apk.read('assets/bin/Data/ScriptingAssemblies.json'))
  index=manifest['names'].index('RebellionRules.dll')
  if manifest['types'][index]!=16 or len(manifest['names'])!=len(manifest['types']):raise ValueError('Extension assembly not registered with Unity')
 return identifier
