import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'highlight'
})

export class HighlightPipeline implements PipeTransform {

    transform(value: any, args: any): any {
        if (!args) {return value;}
        var re = new RegExp(args, 'gi'); 
        return value.replace(re, "<mark>$&</mark>");
    }
}