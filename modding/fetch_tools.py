#!/usr/bin/env python3
import hashlib,json,pathlib,subprocess,tarfile,zipfile,sys
root=pathlib.Path(sys.argv[1]);root.mkdir(parents=True,exist_ok=True)
for name,pin in json.loads(pathlib.Path(__file__).with_name('toolchain.json').read_text()).items():
 p=root/name
 if not p.exists() or hashlib.sha256(p.read_bytes()).hexdigest()!=pin['sha256']:subprocess.run(['curl','-fL','--retry','3','--max-time','180',pin['url'],'-o',str(p)],check=True)
 if hashlib.sha256(p.read_bytes()).hexdigest()!=pin['sha256']:raise SystemExit('Tool hash mismatch '+name)
(root/'dotnet').mkdir(exist_ok=True)
with tarfile.open(root/'dotnet.tar.gz') as t:t.extractall(root/'dotnet',filter='data')
with zipfile.ZipFile(root/'cecil.nupkg') as z:z.extract('lib/netstandard2.0/Mono.Cecil.dll',root/'cecil')
