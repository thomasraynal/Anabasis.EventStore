import { Component, OnInit, AfterViewInit, Input } from '@angular/core';
import { Location } from '@angular/common';
import '../js/modernizr-custom.js';
import { DocumentService } from './document.service';
import { ActivatedRoute, ActivationEnd, NavigationStart, Router } from "@angular/router";
import classie from '../js/classie.js';
import { List } from 'linqts';
import { conditionallyCreateMapObjectLiteral } from '@angular/compiler/src/render3/view/util';
import { Document } from './document.js';
import { DocumentIndex } from './document.index.js';
import { DocumentItem } from './documentItem.js';
import { filter, map } from 'rxjs/operators';
import { MergeScanSubscriber } from 'rxjs/internal/operators/mergeScan';
import { ViewChild } from '@angular/core';
import { AppDocument } from './app.document.js';
import { Subject } from 'rxjs';
import { AppState } from './app.state.js';
import { AppStateService } from './app.state.service.js';
import { BreadCrumb } from './breadcrumb.js';
import { ɵHttpInterceptingHandler } from '@angular/common/http';
import { Statement } from '@angular/compiler';
import { Observable, } from 'rxjs/internal/Observable';
import { zip } from 'rxjs';

@Component({
	selector: 'app-root',
	templateUrl: './app.component.html'
})
export class AppComponent {

	title = 'anabasis';
	MLMenu = null;

	documentIndices: DocumentIndex[] = [];
	navigableDocumentIndices: DocumentIndex[];
	searchPredicate: string;
	currentState: AppState = new AppState(undefined, undefined, undefined);
	breadcrumbs: BreadCrumb[];
	currentParentId: string = null;
	currentTitle: Document;


	constructor(private documentService: DocumentService, private appStateService: AppStateService, private activatedRoute: ActivatedRoute, private router: Router) {

		appStateService.state.subscribe( state => {

			this.documentService.load().then(isSuccess => {

				this.documentService.loadIndices().subscribe(  indicies => {

					this.currentState = state;

					const breadcrumbs = new List<BreadCrumb>();

					breadcrumbs.Add({ name: "Index", link: "/" });

					if (state.document) {

						const document =  documentService.findById(state.document);

						breadcrumbs.Add({ name: document.title, link: "/" + state.document });

					}

					if (state.title) {

						const title =  documentService.findById(state.title);

						breadcrumbs.Add({ name: title.title, link: "/" + state.document + "/" + state.title });

						this.currentTitle = title;
					} else{
						this.currentTitle = null;
					}

					console.log(this.currentTitle);

					this.breadcrumbs = breadcrumbs.ToArray();

				});
			});


		});

		this.documentService.load().then(isSuccess => {

			this.documentService.loadIndices().subscribe(indicies => {

				this.documentIndices = indicies.ToArray();


			});
		});

	}


	searchFromKeyStroke() {
		this.router.navigate(['/search'], { queryParams: { q: this.searchPredicate } });
	}

	onKey(event: any) {
		this.searchPredicate = event.target.value;
	}

}
