import { Component } from '@angular/core';
import '../js/modernizr-custom.js';
import { DocumentService } from './document.service';
import { ActivatedRoute, Router } from "@angular/router";
import { AppState } from './app.state.js';
import { AppStateService } from './app.state.service.js';
import { DocumentIndex } from './document.index.js';
import { BreadCrumb } from './breadcrumb.js';
import { List } from 'linqts';

@Component({
    selector: 'app-menu',
    templateUrl: './app.menu.html',
    styleUrls: []
})
export class AppMenu {

    documentIndices: DocumentIndex[] = [];
    breadcrumbs: BreadCrumb[];

    constructor(private documentService: DocumentService, private router : Router, private appStateService: AppStateService, private activatedRoute: ActivatedRoute) {


        // this.activatedRoute.paramMap.subscribe(
        //     params => {

        //         var document = params['document'];
        //         var title = params['title'];
        //         var subtitle = params['subtitle'];

        //         var state = new AppState(document, title, subtitle);

        //         this.appStateService.change(state);

        //     });

        // this.documentService.load().then(isSuccess => {

        //     this.documentService.loadIndices().subscribe(indicies => {

        //         this.documentIndices = indicies.ToArray();

        //     });
        // });
    }


}