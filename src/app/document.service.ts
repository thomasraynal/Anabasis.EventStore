import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { List } from 'linqts';

import { Document } from './document';
import { DocumentItem } from './documentItem';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {

  documents: List<Document> = null;

  constructor(private http: HttpClient) {
  }

  getDocumentIdAndTitles(): Observable<List<[id: string, title: string]>> {

    return of(this.documents.Select<[id: string, title: string]>(document => [document.id, document.title]));
  }

  getMainTitlesByDocumentId(documentId: string): Observable<List<[id: string, title: string]>> {

    var idAndTitles = this.documents.Where(document => document.id == documentId)
                                      .SelectMany(document => new List<DocumentItem>(document.documentItems))
                                      .Where(documentItem=> documentItem.isMainTitle)
                                      .Select<[id: string, title: string]>(documentItem=> [documentItem.id, documentItem.content]);

    return of(idAndTitles);

  }

  getSecondaryTitlesByMainTitleId(documentId: string, mainTitleId: string): Observable<List<[id: string, title: string]>>  {

    var idAndTitles = this.documents.Where(document => document.id == documentId)
                                      .SelectMany(document => new List<DocumentItem>(document.documentItems))
                                      .Where(documentItem=> documentItem.isSecondaryTitle && documentItem.id == mainTitleId)
                                      .Select<[id: string, title: string]>(documentItem=> [documentItem.id, documentItem.content]);

    return of(idAndTitles);

  }

  getDocumentItemsByMainTitleId(documentId: string, mainTitleId: string) : Observable<List<DocumentItem>> {

    var documentItems = this.documents.Where(document => document.id == documentId)
                                      .SelectMany(document => new List<DocumentItem>(document.documentItems))
                                      .Where(documentItem=> documentItem.mainTitleId == mainTitleId);
                           
     return of(documentItems);
  }

  getDocuments(): Observable<List<Document>> {

    if (this.documents != null) return of(this.documents);

    return this.http.get<Document[]>('assets/export.json', { responseType: 'json' })
      .pipe(map(documents => {
        this.documents = new List<Document>(documents);
        return this.documents;
      }));

  }

}
