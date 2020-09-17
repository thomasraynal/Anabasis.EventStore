import { Injectable } from "@angular/core";  
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";  
import { Observable } from "rxjs";  ;  
import { Document } from './document';
import { DocumentService } from './document.service';
import { List } from 'linqts';
  
@Injectable({ providedIn: 'root' })
export class DocumentsResolve implements Resolve<Promise<Boolean>> {  
  constructor(private documentService: DocumentService) {}  
  
  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<Boolean> {  
    return this.documentService.load();  
  }  
}