import io,zipfile,struct,sys
from pathlib import Path
source,assembly,helper,destination=sys.argv[1:]
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
   data=out.getvalue() if item.filename=="assets/Mono/Managed.zip" else apk.read(item)
   item.extra=b""
   if item.compress_type==zipfile.ZIP_STORED:
    align=16384 if item.filename.endswith(".so") else 4
    start=result.fp.tell()+30+len(item.filename.encode())
    pad=(-start)%align
    if pad and pad<4:pad+=align
    if pad:item.extra=struct.pack("<HH",0xd935,pad-4)+bytes(pad-4)
   result.writestr(item,data)
