import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { List } from 'linqts';

import { Document } from './document';
import { DocumentItem } from './documentItem';
import { DocumentIndex } from './document.index';
import { DocumentSearchResult } from './document.search.result';

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
      .Where(documentItem => documentItem.mainTitleId == mainTitleId || documentItem.id == mainTitleId);

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


  findTitleById(id: string) {

    let title = null;

    var doFindContentById = function (id: string, documentIndice: DocumentIndex) {

      if (documentIndice.id == id) return title = documentIndice.title;
      if (null == documentIndice.documentIndices) return null;

      documentIndice.documentIndices.forEach(indice => {
        var result = doFindContentById(id, indice);
        if (null != result) return result;
      });

      return null;

    };


    this.documentIndices.ForEach(documentIndice => {

      documentIndice.documentIndices.forEach(indice => {
        var result = doFindContentById(id, indice);
        if (null != result) return result;
      });

    });

    return title;

  }

  search(predicate: string): Observable<List<DocumentSearchResult>> {

    var searchResults = this.documents.SelectMany(document => new List<DocumentItem>(document.documentItems))
      .Where(documentItem => documentItem.content.toLowerCase().search(predicate.toLowerCase()) > 0)
      //.GroupBy(documentItem => documentItem.documentId)
      .Select(documentItem => {

        var index = documentItem.content.toLowerCase().search(predicate.toLowerCase());

        var document = this.documents.FirstOrDefault(document => document.id == documentItem.documentId);
        var mainTitle = this.findTitleById(documentItem.mainTitleId);
        var secondaryTitle = this.findTitleById(documentItem.secondaryTitleId);


        let documentSearchResult = new DocumentSearchResult();
        documentSearchResult.peek = documentItem.content.substring(index - 50, index + 50);
        documentSearchResult.documentName = document.title;
        documentSearchResult.mainTitle = mainTitle;
        documentSearchResult.secondaryTitle = secondaryTitle;
        // documentSearchResult






        return documentSearchResult;


      });





    console.log(searchResults);

    return null;

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
