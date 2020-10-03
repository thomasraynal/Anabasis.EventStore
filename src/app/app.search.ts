import { Component } from '@angular/core';
import '../js/modernizr-custom.js';
import { DocumentService } from './document.service';
import { ActivatedRoute, Router } from "@angular/router";
import { DocumentItem } from './documentItem.js';
import { AppState } from './app.state.js';
import { AppStateService } from './app.state.service.js';
import { List } from 'linqts';
import { DocumentSearchResult } from './document.search.result.js';


@Component({
    selector: 'app-search',
    templateUrl: './app.search.html',
    styleUrls: []
})
export class AppSearch {

    documentSearchResults: DocumentSearchResult[];
    searchPredicate: string;

    constructor(private documentService: DocumentService, private activatedRoute: ActivatedRoute) {

        this.activatedRoute.queryParams.subscribe(
            params => {

                this.documentService.load().then(_ => {

                    this.documentService.loadIndices().subscribe(_ => {

                        this.searchPredicate = params['q'];

                        this.documentService.search(this.searchPredicate).subscribe(documentSearchResults => {

                            var byDocument = documentSearchResults.GroupBy(result => result.document);

                            var searchResult = {};

                            for (const document in byDocument) {

                                searchResult[document] = {};

                                var byMainTitle = new List<DocumentSearchResult>(byDocument[document])
                                    .GroupBy(result => result.mainTitle);

                                for (const mainTitle in byMainTitle) {

                                    var bySecondaryTitle = new List<DocumentSearchResult>(byMainTitle[mainTitle])
                                        .GroupBy(result => result.secondaryTitle);

                                    searchResult[document][mainTitle] = bySecondaryTitle;

                                }
                            }

                            console.log(searchResult);

                            this.documentSearchResults = documentSearchResults.ToArray();

                        });
                    });
                });
            });
    }



}