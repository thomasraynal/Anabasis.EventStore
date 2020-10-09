import { Component } from '@angular/core';
import '../js/modernizr-custom.js';
import { DocumentService } from './document.service';
import { ActivatedRoute, Router } from "@angular/router";
import { List } from 'linqts';
import { DocumentIndex } from './document.index.js';
import { AppState } from './app.state.js';
import { AppStateService } from './app.state.service.js';

import { BreadCrumb } from './breadcrumb.js';

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
	currentTitle: DocumentIndex;


	constructor(private documentService: DocumentService, appStateService: AppStateService, private router: Router) {

		appStateService.state.subscribe( state => {

			this.documentService.load().then(() => {

				this.documentService.loadIndices().subscribe(  () => {

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

		this.documentService.load().then(() => {

			this.documentService.loadIndices().subscribe(indicies => {

				this.documentIndices = indicies.ToArray();

				const breadcrumbs = new List<BreadCrumb>();

				breadcrumbs.Add({ name: "Index", link: "/" });

				this.breadcrumbs = breadcrumbs.ToArray();


			});
		});

	}

	onKey(event: any) {
		this.searchPredicate = event.target.value;
	}

}
