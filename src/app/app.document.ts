import { Component } from '@angular/core';
import '../js/modernizr-custom.js';
import { DocumentService } from './document.service';
import { ActivatedRoute } from "@angular/router";
import { DocumentItem } from './documentItem.js';
import { AppState } from './app.state.js';
import { AppStateService } from './app.state.service.js';


@Component({
    selector: 'app-document',
    templateUrl: './app.document.html',
    styleUrls: []
})
export class AppDocument {


    content: DocumentItem[];
    textToHighlight: string;

    constructor(private documentService: DocumentService, private appStateService: AppStateService, private activatedRoute: ActivatedRoute) {

        this.activatedRoute.params.subscribe(
            params => {

                this.activatedRoute.queryParams.subscribe(
                    queryParams => {

                        var document = params['document'];
                        var title = params['title'];
                        var subtitle = params['subtitle'];

                        this.textToHighlight = queryParams['h'];

                        var state = new AppState(document, title, subtitle);

                        this.appStateService.change(state);

                        this.documentService.load().then(_ => {

                            this.documentService.loadIndices().subscribe(_ => {

                                this.load(state);

                                this.navigateToAnchor(state.subtitle);

                            });
                        });
                    });

            });

    }

    navigateToAnchor(anchor: string) {

        if (!anchor) return;

        setTimeout(function () {

            let testElement = document.getElementById('anchor-' + anchor);
            if (testElement != undefined) testElement.scrollIntoView();

        }, 200);

    }

    load(state: AppState) {

        if (!state) return;
        if (!state.document) return;
        if (!state.title) return;

        var self = this;

        this.documentService.getDocumentItemsByMainTitleIdWithDocumentTitle(state.document, state.title)
            .subscribe(documents => {
                self.content = documents.ToArray();
            });

    }

}