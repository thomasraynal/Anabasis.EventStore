import { Component } from '@angular/core';
import '../js/modernizr-custom.js';
import { DocumentService } from './document.service';
import { ActivatedRoute } from "@angular/router";
import { AppState } from './app.state.js';
import { AppStateService } from './app.state.service.js';

@Component({
    selector: 'app-menu',
    templateUrl: './app.menu.html',
    styleUrls: []
})
export class AppMenu {

    constructor(private documentService: DocumentService, private appStateService: AppStateService, private activatedRoute: ActivatedRoute) {


    }


}