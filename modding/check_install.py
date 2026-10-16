#!/usr/bin/env python3
"""Validate installation-critical ZIP/AXML properties, without claiming device install."""
import argparse,pathlib,struct,zipfile,subprocess
from install_compat import attributes,prepare_manifest

def verify(source,output):
    with zipfile.ZipFile(source) as src,zipfile.ZipFile(output) as dst:
        assert dst.testzip() is None
        assert dst.read('AndroidManifest.xml')==prepare_manifest(src.read('AndroidManifest.xml'))
        attrs={(e,n):(t,v) for e,n,t,v,o in attributes(dst.read('AndroidManifest.xml'))}
        assert attrs['manifest','installLocation']==(16,0)
        assert attrs['application','debuggable']==(18,0)
        assert attrs['uses-sdk','minSdkVersion']==(16,23)
        assert attrs['uses-sdk','targetSdkVersion']==(16,35)
        assert attrs['application','extractNativeLibs']==(18,0xffffffff)
        for name in ['AndroidManifest.xml','resources.arsc']:
            i=dst.getinfo(name);assert i.compress_type==zipfile.ZIP_STORED
            with open(output,'rb') as f:
                f.seek(i.header_offset+26);namesize,extrasize=struct.unpack('<HH',f.read(4))
            assert (i.header_offset+30+namesize+extrasize)%4==0
        assert len(dst.namelist())==len(set(dst.namelist()))
    print('PASS: binary manifest, auto install location, non-debuggable, SDK values, extraction flags, stored/aligned manifest and resources, unique ZIP entries')

if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('source');ap.add_argument('output');ap.add_argument('--aapt2');args=ap.parse_args();verify(args.source,args.output)
    if args.aapt2:
        result=subprocess.run([args.aapt2,'dump','badging',args.output],capture_output=True,text=True,check=True)
        if "package: name='com.mkarpenko.worldbox'" not in result.stdout or "launchable-activity: name='com.unity3d.player.UnityPlayerActivity'" not in result.stdout:raise SystemExit('Unexpected aapt2 package/activity')
        print('PASS: Android SDK aapt2 reads package, resources and launcher activity')
