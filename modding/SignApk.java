import com.android.apksig.*;
import java.io.*;
import java.security.*;
import java.security.cert.X509Certificate;
import java.util.*;
public class SignApk {
 public static void main(String[] args) throws Exception {
  if(args[0].equals("verify")) {
   ApkVerifier.Result r=new ApkVerifier.Builder(new File(args[1])).setMinCheckedPlatformVersion(21).build().verify();
   System.out.println("Verified: "+r.isVerified()+" v1="+r.isVerifiedUsingV1Scheme()+" v2="+r.isVerifiedUsingV2Scheme()+" v3="+r.isVerifiedUsingV3Scheme());
   for(Object e:r.getErrors())System.out.println(e);if(!r.isVerified())System.exit(1);return;
  }
  KeyStore k=KeyStore.getInstance("PKCS12");try(InputStream i=new FileInputStream(args[2])){k.load(i,"android".toCharArray());}
  PrivateKey key=(PrivateKey)k.getKey("worldbox-test","android".toCharArray());
  X509Certificate cert=(X509Certificate)k.getCertificate("worldbox-test");
  ApkSigner.SignerConfig config=new ApkSigner.SignerConfig.Builder("worldbox-test",key,Arrays.asList(cert)).build();
  new ApkSigner.Builder(Arrays.asList(config)).setInputApk(new File(args[0])).setOutputApk(new File(args[1])).setMinSdkVersion(21).setV1SigningEnabled(true).setV2SigningEnabled(true).setV3SigningEnabled(true).build().sign();
 }
}
