import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { List } from 'linqts';

import { Document } from './document';
import { DocumentItem } from './documentItem';
import { DocumentIndex } from './document.index';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {

  documents: List<Document> = null;
  documentIndices: List<DocumentIndex> = null;

  constructor(private http: HttpClient) {
  }

  getDocumentsIdAndTitle(): Observable<List<[id: string, title: string]>> {

    return of(this.documents.Select<[id: string, title: string]>(document => [document.id, document.title]));
  }

  getMainTitlesByDocumentId(documentId: string): Observable<List<[id: string, title: string]>> {

    var idAndTitles = this.documents.Where(document => document.id == documentId)
      .SelectMany(document => new List<DocumentItem>(document.documentItems))
      .Where(documentItem => documentItem.isMainTitle)
      .Select<[id: string, title: string]>(documentItem => [documentItem.id, documentItem.content]);

    return of(idAndTitles);

  }

  getSecondaryTitlesByMainTitleId(documentId: string, mainTitleId: string): Observable<List<[id: string, title: string]>> {

    var idAndTitles = this.documents.Where(document => document.id == documentId)
      .SelectMany(document => new List<DocumentItem>(document.documentItems))
      .Where(documentItem => documentItem.isSecondaryTitle && documentItem.id == mainTitleId)
      .Select<[id: string, title: string]>(documentItem => [documentItem.id, documentItem.content]);

    return of(idAndTitles);

  }

  getDocumentItemsByMainTitleIdWithDocumentTitle(documentId: string, mainTitleId: string): Observable<List<DocumentItem>> {

    var documentItems = this.documents.Where(document => document.id == documentId)
      .SelectMany(document => new List<DocumentItem>(document.documentItems))
      .Where(documentItem => documentItem.mainTitleId == mainTitleId ||documentItem.id == mainTitleId );

    return of(documentItems);
  }

  getDocumentItemsBySecondaryTitleId(documentId: string, secondaryTitleId: string): Observable<List<DocumentItem>> {

    var documentItems = this.documents.Where(document => document.id == documentId)
      .SelectMany(document => new List<DocumentItem>(document.documentItems))
      .Where(documentItem => documentItem.secondaryTitleId == secondaryTitleId);

    return of(documentItems);
  }

  load(): Promise<Boolean> {

    return new Promise((resolve, reject) => {

      this.loadDocuments().subscribe(_ => {
        this.loadIndices().subscribe(_ => {
          resolve.apply(true);
        });
      });

    });
  }

  loadIndices(): Observable<List<DocumentIndex>> {

    if (this.documentIndices != null) return of(this.documentIndices);

    return this.http.get<DocumentIndex[]>('assets/index.json', { responseType: 'json' })
      .pipe(map(documentIndices => {
        this.documentIndices = new List<DocumentIndex>(documentIndices);
        return this.documentIndices;
      }));

  }


  loadDocuments(): Observable<List<Document>> {

    if (this.documents != null) return of(this.documents);

    return this.http.get<Document[]>('assets/export.json', { responseType: 'json' })
      .pipe(map(documents => {
        this.documents = new List<Document>(documents);
        return this.documents;
      }));

  }

}
