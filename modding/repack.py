import io,zipfile,struct,sys
from pathlib import Path
from install_compat import prepare_manifest
from runtime_cache import cache_id,patch_bridge,scripting_manifest
source,assembly,helper,destination=sys.argv[1:]
identifier=cache_id(Path(assembly).read_bytes(),Path(helper).read_bytes())
with zipfile.ZipFile(source) as apk:
 raw=apk.read("assets/Mono/Managed.zip")
 out=io.BytesIO()
 with zipfile.ZipFile(io.BytesIO(raw)) as managed,zipfile.ZipFile(out,"w",zipfile.ZIP_DEFLATED) as target:
  for item in managed.infolist():
   data=Path(assembly).read_bytes() if item.filename=="Assembly-CSharp.dll" else managed.read(item)
   target.writestr(item,data)
  target.writestr("RebellionRules.dll",Path(helper).read_bytes())
 with zipfile.ZipFile(destination,"w") as result:
  for item in apk.infolist():
   if item.filename.startswith("META-INF/") and (item.filename.endswith((".SF",".RSA",".DSA",".EC")) or item.filename=="META-INF/MANIFEST.MF"):continue
   if item.filename.endswith('/.DS_Store') or item.filename.startswith('__MACOSX/'):continue
   data=out.getvalue() if item.filename=="assets/Mono/Managed.zip" else apk.read(item)
   if item.filename=='lib/arm64-v8a/libil2cpp.so':data=patch_bridge(data,identifier)
   if item.filename=='assets/bin/Data/ScriptingAssemblies.json':data=scripting_manifest(data)
   if item.filename=='AndroidManifest.xml':
    data=prepare_manifest(data)
    item.compress_type=zipfile.ZIP_STORED
   item.extra=b""
   if item.compress_type==zipfile.ZIP_STORED:
    align=16384 if item.filename.endswith(".so") else 4
    start=result.fp.tell()+30+len(item.filename.encode())
    pad=(-start)%align
    while pad and pad<6:pad+=align
    if pad:item.extra=struct.pack("<HHH",0xd935,pad-4,align)+bytes(pad-6)
   result.writestr(item,data)
