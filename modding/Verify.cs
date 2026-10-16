using System;using System.Linq;using System.Collections.Generic;using Mono.Cecil;using Mono.Cecil.Cil;
class Verify {
 static int Count(StackBehaviour s){switch(s){case StackBehaviour.Pop0:case StackBehaviour.Push0:return 0;case StackBehaviour.Pop1:case StackBehaviour.Popi:case StackBehaviour.Popref:case StackBehaviour.Push1:case StackBehaviour.Pushi:case StackBehaviour.Pushi8:case StackBehaviour.Pushr4:case StackBehaviour.Pushr8:case StackBehaviour.Pushref:return 1;case StackBehaviour.Pop1_pop1:case StackBehaviour.Popi_pop1:case StackBehaviour.Popi_popi:case StackBehaviour.Popi_popi8:case StackBehaviour.Popi_popr4:case StackBehaviour.Popi_popr8:case StackBehaviour.Popref_pop1:case StackBehaviour.Popref_popi:case StackBehaviour.Push1_push1:return 2;case StackBehaviour.Popi_popi_popi:case StackBehaviour.Popref_popi_popi:case StackBehaviour.Popref_popi_popi8:case StackBehaviour.Popref_popi_popr4:case StackBehaviour.Popref_popi_popr8:case StackBehaviour.Popref_popi_popref:return 3;default:throw new Exception("Unknown stack behaviour "+s);}}
 static void Check(MethodDefinition m){var seen=new Dictionary<Instruction,int>();var queue=new Queue<KeyValuePair<Instruction,int>>();Action<Instruction,int> Add=(i,n)=>{if(i!=null)queue.Enqueue(new KeyValuePair<Instruction,int>(i,n));};Add(m.Body.Instructions.First(),0);foreach(var e in m.Body.ExceptionHandlers){Add(e.HandlerStart,e.HandlerType==ExceptionHandlerType.Catch||e.HandlerType==ExceptionHandlerType.Filter?1:0);if(e.FilterStart!=null)Add(e.FilterStart,1);}
  while(queue.Count>0){var item=queue.Dequeue();var i=item.Key;int depth=item.Value,prior;if(seen.TryGetValue(i,out prior)){if(prior!=depth)throw new Exception(m.FullName+" inconsistent stack at "+i+": "+prior+" / "+depth);continue;}seen[i]=depth;int pop,push;
   if(i.OpCode.StackBehaviourPop==StackBehaviour.Varpop){if(i.OpCode==OpCodes.Ret)pop=m.ReturnType.MetadataType==MetadataType.Void?0:1;else if(i.Operand is MethodReference){var method=(MethodReference)i.Operand;pop=method.Parameters.Count+(method.HasThis&&i.OpCode!=OpCodes.Newobj?1:0);}else if(i.Operand is CallSite){var call=(CallSite)i.Operand;pop=call.Parameters.Count+(call.HasThis?1:0)+1;}else throw new Exception("Unknown varpop "+i);}else pop=i.OpCode.StackBehaviourPop==StackBehaviour.PopAll?depth:Count(i.OpCode.StackBehaviourPop);
   if(i.OpCode.StackBehaviourPush==StackBehaviour.Varpush){var method=i.Operand as MethodReference;var call=i.Operand as CallSite;push=(method!=null?method.ReturnType:call.ReturnType).MetadataType==MetadataType.Void?0:1;}else push=Count(i.OpCode.StackBehaviourPush);
   if(depth<pop)throw new Exception(m.FullName+" stack underflow at "+i);int after=depth-pop+push;if(i.OpCode==OpCodes.Ret){if(after!=0)throw new Exception(m.FullName+" unbalanced return "+after);continue;}
   if(i.OpCode==OpCodes.Leave||i.OpCode==OpCodes.Leave_S)after=0;
   if(i.OpCode==OpCodes.Endfinally||i.OpCode==OpCodes.Endfilter||i.OpCode.FlowControl==FlowControl.Throw)continue;
   if(i.Operand is Instruction)Add((Instruction)i.Operand,after);if(i.Operand is Instruction[])foreach(var target in (Instruction[])i.Operand)Add(target,after);
   if(i.OpCode.FlowControl!=FlowControl.Branch)Add(i.Next,after);
  }
 }

 static string Shape(MethodDefinition f){if(!f.HasBody)return "";var ins=f.Body.Instructions;return String.Join(";",ins.Select(i=>i.OpCode.Name+":"+(i.Operand is Instruction ? "@"+ins.IndexOf((Instruction)i.Operand) : i.Operand is Instruction[] ? String.Join(",",((Instruction[])i.Operand).Select(x=>ins.IndexOf(x))) : Convert.ToString(i.Operand))));}
 static IEnumerable<TypeDefinition> Types(ModuleDefinition m){foreach(var t in m.Types){yield return t;foreach(var n in Nested(t))yield return n;}}
 static IEnumerable<TypeDefinition> Nested(TypeDefinition t){foreach(var n in t.NestedTypes){yield return n;foreach(var q in Nested(n))yield return q;}}
 static void Main(string[] a){
 var r=new DefaultAssemblyResolver();r.AddSearchDirectory(a[3]);r.AddSearchDirectory(System.IO.Path.GetDirectoryName(a[2]));var rp=new ReaderParameters{AssemblyResolver=r};
 var before=ModuleDefinition.ReadModule(a[0],rp);var after=ModuleDefinition.ReadModule(a[1],rp);var h=ModuleDefinition.ReadModule(a[2],rp);
 var expected=new HashSet<string>{"BaseUnlockableAsset::isUnlocked","BaseUnlockableAsset::isAvailable","BaseUnlockableAsset::isUnlockedByPlayer","AssetManager::initLibs","ItemLibrary::fillUnlockedPools","AugmentationsEditor`7::editorButtonClick","GeneEditor::clickGeneAssetAction","LocusElement::OnDrop"};
 int changed=0,refs=0;
 foreach(var t in Types(after))foreach(var f in t.Methods){var original=Types(before).Single(x=>x.FullName==t.FullName).Methods.Single(x=>x.FullName==f.FullName);if(Shape(original)!=Shape(f)){var id=t.Name+"::"+f.Name;if(!expected.Remove(id))throw new Exception("Unexpected changed method: "+id);Check(f);changed++;Console.WriteLine("Verified stack graph: "+id);}
 if(!f.HasBody)continue;foreach(var i in f.Body.Instructions){if(i.Operand is Instruction target && !f.Body.Instructions.Contains(target))throw new Exception("Invalid branch");var mr=i.Operand as MethodReference;if(mr==null||mr.DeclaringType.Scope.Name!="NativeContentUnlocks")continue;var definition=h.Types.Single(x=>x.FullName==mr.DeclaringType.FullName).Methods.Single(x=>x.FullName==mr.FullName);refs++;}}
 if(expected.Count!=0||changed!=8||refs!=5)throw new Exception("Coverage mismatch "+changed+"/"+refs+" missing "+String.Join(",",expected));
 foreach(var t in h.Types)foreach(var f in t.Methods)if(f.HasBody){Check(f);foreach(var i in f.Body.Instructions){var mr=i.Operand as MethodReference;if(mr!=null&&mr.DeclaringType.Scope.Name=="Assembly-CSharp"&&mr.Resolve()==null)throw new Exception("Unresolved game method: "+mr);var fr=i.Operand as FieldReference;if(fr!=null&&fr.DeclaringType.Scope.Name=="Assembly-CSharp"&&fr.Resolve()==null)throw new Exception("Unresolved game field: "+fr);}}
 if(after.AssemblyReferences.Any(x=>x.Name=="RebellionRules"))throw new Exception("Old mod dependency present");
 Console.WriteLine("PASS: exactly 8 intended game methods, 5 helper calls, helper stack graphs and native references.");
 }
}
