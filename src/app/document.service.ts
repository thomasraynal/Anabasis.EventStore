import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { List } from 'linqts';

import { Document } from './document';
import { DocumentItem } from './documentItem';
import { DocumentIndex } from './document.index';
import { DocumentSearchResult } from './document.search.result';
import { Env } from './env';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {

  documents: List<Document> = null;
  documentIndices: List<DocumentIndex> = null;

  constructor(private http: HttpClient) {
  }

  async getDocumentById(id: string): Promise<Document> {
    return this.documents.FirstOrDefault(document => document.id == id);
  }

  getDocumentsIdAndTitle(): Observable<List<[id: string, title: string]>> {

    return of(this.documents.Select<[id: string, title: string]>(document => [document.id, document.title]));
  }

  getMainTitlesByDocumentId(documentId: string): Observable<List<[id: string, title: string]>> {

    var idAndTitles = this.documents.Where(document => document.id == documentId)
      .SelectMany(document => new List<DocumentItem>(document.children))
      .Where(documentItem => documentItem.isMainTitle)
      .Select<[id: string, title: string]>(documentItem => [documentItem.id, documentItem.content]);

    return of(idAndTitles);

  }

  getSecondaryTitlesByMainTitleId(documentId: string, mainTitleId: string): Observable<List<[id: string, title: string]>> {

    var idAndTitles = this.documents.Where(document => document.id == documentId)
      .SelectMany(document => new List<DocumentItem>(document.children))
      .Where(documentItem => documentItem.isSecondaryTitle && documentItem.id == mainTitleId)
      .Select<[id: string, title: string]>(documentItem => [documentItem.id, documentItem.content]);

    return of(idAndTitles);

  }

  getDocumentItemsByMainTitleIdWithDocumentTitle(documentId: string, mainTitleId: string): Observable<List<DocumentItem>> {

    var documentItems = this.documents.Where(document => document.id == documentId)
      .SelectMany(document => new List<DocumentItem>(document.children))
      .Where(documentItem => documentItem.mainTitleId == mainTitleId || documentItem.id == mainTitleId);

    return of(documentItems);
  }

  getDocumentItemsBySecondaryTitleId(documentId: string, secondaryTitleId: string): Observable<List<DocumentItem>> {

    var documentItems = this.documents.Where(document => document.id == documentId)
      .SelectMany(document => new List<DocumentItem>(document.children))
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


  findById(id: string): DocumentIndex {

    let title = null;

    var doFindContentById = function (id: string, documentIndice: DocumentIndex) {

      if (documentIndice.id == id) return title = documentIndice;
      if (null == documentIndice.children) return null;

      documentIndice.children.forEach(indice => {
        var result = doFindContentById(id, indice);
        if (null != result) return result;
      });

      return null;

    };

     
    this.documentIndices.ForEach(documentIndice => {

      if (documentIndice.id == id) return title = documentIndice;

      documentIndice.children.forEach(indice => {
        var result = doFindContentById(id, indice);
        if (null != result) return result;
      });

    });

    return title;

  }

  search(predicate: string): Observable<List<DocumentSearchResult>> {

    var searchResults = this.documents.SelectMany(document => new List<DocumentItem>(document.children))
      .Where(documentItem =>
        !documentItem.isSecondaryTitle && !documentItem.isMainTitle && documentItem.parentId &&
        documentItem.content.toLowerCase().search(predicate.toLowerCase()) > 0)
      .Select(documentItem => {

        var index = documentItem.content.toLowerCase().search(predicate.toLowerCase());

        var document = this.documents.FirstOrDefault(document => document.id == documentItem.documentId);
        var mainTitle = this.findById(documentItem.mainTitleId);
        var secondaryTitle = this.findById(documentItem.secondaryTitleId);

        let documentSearchResult = new DocumentSearchResult();
        documentSearchResult.peek = documentItem.content.substring(index - 50, index + 50);

        documentSearchResult.document = document.title;
        documentSearchResult.documentId = document.id;

        if (mainTitle) {
          documentSearchResult.mainTitle = mainTitle.title;
          documentSearchResult.mainTitleId = documentItem.mainTitleId;
        }

        if (secondaryTitle) {
          documentSearchResult.secondaryTitle = secondaryTitle.title;
          documentSearchResult.secondaryTitleId = documentItem.secondaryTitleId;
        }

        documentSearchResult.predicate = predicate;

        return documentSearchResult;

      });

    return of(searchResults);

  }

  loadIndices(): Observable<List<DocumentIndex>> {

    if (this.documentIndices != null) return of(this.documentIndices);

    return this.http.get<DocumentIndex[]>('assets/'+Env.source+'/index.json', { responseType: 'json' })
      .pipe(map(documentIndices => {
        this.documentIndices = new List<DocumentIndex>(documentIndices);
        return this.documentIndices;
      }));

  }


  loadDocuments(): Observable<List<Document>> {

    if (this.documents != null) return of(this.documents);

    return this.http.get<Document[]>('assets/'+Env.source+'/export.json', { responseType: 'json' })
      .pipe(map(documents => {
        this.documents = new List<Document>(documents);
        return this.documents;
      }));

  }

}
